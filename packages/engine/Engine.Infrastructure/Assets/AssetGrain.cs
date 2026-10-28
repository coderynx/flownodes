using System.Text.Json;
using Engine.Domain.Alarms;
using Engine.Infrastructure.Alarms;
using Engine.Infrastructure.Connections;
using Engine.Infrastructure.DataPoints;
using Engine.Infrastructure.Devices;
using Engine.Infrastructure.Flows;
using Engine.Infrastructure.Resources;
using Flownodes.Sdk;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace Engine.Infrastructure.Assets;

[Reentrant]
internal sealed class AssetGrain(
    [PersistentState("asset")] IPersistentState<ResourceState> state,
    [PersistentState("assetvalues")] IPersistentState<AssetValues> values,
    [PersistentState("assetalarms")] IPersistentState<AlarmState> alarmState,
    IAlarmHistory history,
    IStatusReporter reporter,
    IOptions<AssetGrainOptions> options,
    ILogger<AssetGrain> logger) : ResourceGrain(state, reporter, options.Value.StatusInterval, "Asset"), IAssetGrain
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string, DeviceFeed> _feeds = [];
    private IReadOnlyList<AssetDataPoint> _points = [];
    private AssetTypeInfo? _assetType;
    private string? _registeredWith;
    private AssetAlarms? _alarms;
    private readonly Dictionary<Guid, Alarm> _unrecorded = [];
    private readonly SemaphoreSlim _alarmWrite = new(1, 1);
    private IDisposable? _alarmTimer;
    private string _persistedPending = "";
    private bool _alarmsAnnounced;
    private readonly Dictionary<IFlowWatcher, HashSet<string>> _watchers = [];
    private readonly Dictionary<string, string?> _watchedValues = [];

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (IsLive)
        {
            await RegisterAsync();
        }
    }

    public async Task ApplyAsync(Guid uid, long generation, long seq, string spec)
    {
        var previousType = _registeredWith;
        var sameIncarnation = State.Uid == uid;
        if (!await AcceptAsync(uid, generation, seq, spec))
        {
            return;
        }

        if (!sameIncarnation)
        {
            await ClearWrittenAsync();
            await CommitAlarmsAsync(Alarms.RetireAll(DateTimeOffset.UtcNow));
        }

        if (previousType is not null && previousType != AssetTypeName)
        {
            await AssetType(previousType).UnregisterInstanceAsync(ResourceName);
        }

        await RegisterAsync();
    }

    public async Task DeleteAsync(Guid uid, long seq)
    {
        var assetType = _registeredWith;
        if (!await TombstoneAsync(uid, seq))
        {
            return;
        }

        if (assetType is not null)
        {
            await AssetType(assetType).UnregisterInstanceAsync(ResourceName);
        }

        foreach (var device in _feeds.Keys.ToList())
        {
            await Unsubscribe(device);
        }

        await ClearWrittenAsync();
        _alarmTimer?.Dispose();
        await RetireAlarmsAsync();
        DeactivateOnIdle();
    }

    public async Task PingAsync()
    {
        if (IsLive)
        {
            await RegisterAsync();
        }
    }

    public async Task AssetTypeChangedAsync(AssetTypeInfo assetType)
    {
        if (!IsLive)
        {
            return;
        }

        _assetType = assetType;
        Refresh();
        await SyncDevicesAsync();
        await SyncAlarmsAsync(true);
        NotifyWatchers();
        await ReportStatusAsync();
    }

    public async Task DeviceChangedAsync(string device, DeviceFeed feed)
    {
        if (IsLive && _feeds.ContainsKey(device))
        {
            _feeds[device] = feed;
            await SyncAlarmsAsync(false);
            NotifyWatchers();
        }

        await ReportStatusAsync();
    }

    public Task<IReadOnlyList<AlarmSnapshot>?> GetAlarmsAsync() =>
        Task.FromResult<IReadOnlyList<AlarmSnapshot>?>(IsLive ? [.. Alarms.Live.Select(AlarmSnapshot.From)] : null);

    public async Task<bool> AcknowledgeAsync(Guid id, Guid by, string? comment)
    {
        var alarm = IsLive ? Alarms.Acknowledge(id, by, comment, DateTimeOffset.UtcNow) : null;
        if (alarm is null)
        {
            return false;
        }

        await CommitAlarmsAsync([alarm]);

        return true;
    }

    public Task<IReadOnlyList<DataPointSnapshot>?> GetDataPointsAsync() =>
        Task.FromResult<IReadOnlyList<DataPointSnapshot>?>(
            IsLive ? [.. _points.Select(point => Snapshot(point, Evaluate(point, [])))] : null);

    public async Task<CallResult> WriteAsync(string dataPoint, string value)
    {
        var point = _points.FirstOrDefault(candidate => candidate.Name == dataPoint);
        if (!IsLive || point is null)
        {
            return CallResult.Failure("NotFound", $"The Asset has no Data Point '{dataPoint}'.");
        }

        if (!point.Writable)
        {
            return CallResult.Failure("ReadOnly", $"Data Point '{dataPoint}' is read-only.");
        }

        using var document = JsonDocument.Parse(value);
        if (ValueCheck.Problem(point.Type, point.Min, point.Max, point.Values, document.RootElement) is { } problem)
        {
            return CallResult.Failure("InvalidValue", problem);
        }

        if (point.Source is { Device: { } device, DeviceDataPoint: { } deviceDataPoint })
        {
            return await Device(device).WriteAsync(deviceDataPoint, value);
        }

        values.State.Written[dataPoint] = new WrittenValue(document.RootElement.GetRawText(), DateTimeOffset.UtcNow);
        await values.WriteStateAsync();
        await SyncAlarmsAsync(false);
        NotifyWatchers();

        return CallResult.Success();
    }

    public Task<WatchResult> WatchDataPointAsync(IFlowWatcher watcher, string dataPoint)
    {
        var point = _points.FirstOrDefault(candidate => candidate.Name == dataPoint);
        if (!IsLive || point is null)
        {
            return Task.FromResult(WatchResult.Missing);
        }

        if (!_watchers.TryGetValue(watcher, out var watched))
        {
            _watchers[watcher] = watched = [];
        }

        watched.Add(dataPoint);
        var snapshot = Snapshot(point, Evaluate(point, []));
        _watchedValues.TryAdd(dataPoint, snapshot.Value);

        return Task.FromResult(new WatchResult(true, snapshot));
    }

    public Task UnwatchAsync(IFlowWatcher watcher)
    {
        _watchers.Remove(watcher);

        return Task.CompletedTask;
    }

    protected override JsonElement BuildStatus()
    {
        var (reason, ready, message) = Condition();

        return JsonSerializer.SerializeToElement(
            new
            {
                conditions = new[] { new { type = "Ready", status = ready ? "True" : "False", reason, message } },
                dataPoints = ready ? _points.Select(point => point.Name).ToArray() : [],
                alarms = new
                {
                    low = AlarmCount(AlarmPriority.Low),
                    medium = AlarmCount(AlarmPriority.Medium),
                    high = AlarmCount(AlarmPriority.High),
                    critical = AlarmCount(AlarmPriority.Critical)
                }
            },
            Web);
    }

    private string? AssetTypeName =>
        Spec.TryGetProperty("assetType", out var name) && name.ValueKind == JsonValueKind.String
            ? name.GetString()
            : null;

    private static DataPointSnapshot Snapshot(AssetDataPoint point, Evaluated evaluated) =>
        new(
            point.Name,
            point.Type,
            point.Writable,
            point.Unit,
            point.Min,
            point.Max,
            point.Values,
            evaluated.Value,
            evaluated.Quality,
            evaluated.Timestamp);

    private IAssetTypeGrain AssetType(string name) =>
        GrainFactory.GetGrain<IAssetTypeGrain>($"{Namespace}/{name}");

    private IDeviceGrain Device(string name) => GrainFactory.GetGrain<IDeviceGrain>($"{Namespace}/{name}");

    private (string Reason, bool Ready, string? Message) Condition()
    {
        if (AssetTypeName is { } name && _assetType is not { Exists: true })
        {
            return ("AssetTypeMissing", false, $"Asset Type '{name}' is not there.");
        }

        var missing = _points.Where(IsMissing)
            .Select(point => $"'{point.Source!.DeviceDataPoint}' of Device '{point.Source.Device}'")
            .ToList();

        return missing.Count > 0
            ? ("DataPointMissing", false, $"No longer a Data Point: {string.Join(", ", missing)}.")
            : ("Resolved", true, null);
    }

    private bool IsMissing(AssetDataPoint point) =>
        point.Source is { Device: { } device, DeviceDataPoint: { } dataPoint } &&
        _feeds.TryGetValue(device, out var feed) && feed is { Exists: true, SurfaceKnown: true } &&
        feed.DataPoints.All(snapshot => snapshot.Name != dataPoint);

    private Evaluated Evaluate(AssetDataPoint point, HashSet<string> visiting)
    {
        if (point.Source is null)
        {
            return values.State.Written.TryGetValue(point.Name, out var written)
                ? new Evaluated(written.Value, (int)Quality.Good, written.Timestamp)
                : Evaluated.Unknown;
        }

        return point.Source.Device is { } device
            ? Mapped(device, point.Source.DeviceDataPoint!)
            : Computed(point, visiting);
    }

    private Evaluated Mapped(string device, string dataPoint)
    {
        if (!_feeds.TryGetValue(device, out var feed))
        {
            return Evaluated.Unknown;
        }

        if (!feed.Exists)
        {
            return Evaluated.Bad;
        }

        var snapshot = feed.DataPoints.FirstOrDefault(candidate => candidate.Name == dataPoint);
        if (snapshot is null)
        {
            return Evaluated.Bad;
        }

        return snapshot.Value is null
            ? new Evaluated(null, Math.Max(snapshot.Quality, (int)Quality.Uncertain), null)
            : new Evaluated(snapshot.Value, snapshot.Quality, snapshot.Timestamp);
    }

    private Evaluated Computed(AssetDataPoint point, HashSet<string> visiting)
    {
        if (!visiting.Add(point.Name))
        {
            return Evaluated.Bad;
        }

        var inputs = point.Source!.Inputs.Select(name => _points.FirstOrDefault(candidate => candidate.Name == name))
            .Select(input => input is null ? Evaluated.Bad : Evaluate(input, visiting))
            .ToList();
        visiting.Remove(point.Name);

        var quality = inputs.Max(input => input.Quality);
        if (inputs.Any(input => input.Value is null))
        {
            return new Evaluated(null, Math.Max(quality, (int)Quality.Uncertain), null);
        }

        var numbers = inputs.Select(input => JsonDocument.Parse(input.Value!).RootElement.GetDouble()).ToList();
        var result = point.Source.Operation switch
        {
            "min" => numbers.Min(),
            "max" => numbers.Max(),
            "average" => numbers.Average(),
            _ => numbers.Sum()
        };

        return new Evaluated(
            JsonSerializer.Serialize(result),
            quality,
            inputs.Where(input => input.Timestamp is not null).Min(input => input.Timestamp));
    }

    private void NotifyWatchers()
    {
        var names = _watchers.Values.SelectMany(watched => watched).ToHashSet();
        foreach (var point in _points.Where(point => names.Contains(point.Name)))
        {
            var snapshot = Snapshot(point, Evaluate(point, []));
            if (_watchedValues.TryGetValue(point.Name, out var known) && known == snapshot.Value)
            {
                continue;
            }

            _watchedValues[point.Name] = snapshot.Value;
            var watchers = _watchers.Where(entry => entry.Value.Contains(point.Name)).Select(entry => entry.Key);
            foreach (var watcher in watchers)
            {
                watcher.DataPointChangedAsync("Asset", ResourceName, snapshot).Ignore();
            }
        }
    }

    private void Refresh() => _points = AssetDataPoint.Merge(_assetType?.DataPoints ?? [], Spec);

    private async Task RegisterAsync()
    {
        _registeredWith = AssetTypeName;
        _assetType = AssetTypeName is { } name ? await AssetType(name).RegisterInstanceAsync(ResourceName) : null;
        Refresh();
        await SyncDevicesAsync();
        await SyncAlarmsAsync(true);
        await ReportStatusAsync();
    }

    private AssetAlarms Alarms => _alarms ??= alarmState.State.ToAlarms(ResourceName);
    private int AlarmCount(AlarmPriority priority) => Alarms.Live.Count(alarm => alarm.Priority == priority);

    private async Task SyncAlarmsAsync(bool conditionsChanged)
    {
        if (!IsLive)
        {
            return;
        }

        if (!_alarmsAnnounced)
        {
            _alarmsAnnounced = true;
            foreach (var alarm in Alarms.Live)
            {
                _unrecorded[alarm.Id] = alarm;
            }
        }

        if (AssetTypeName is not null && _assetType is not { Exists: true })
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var changed = new List<Alarm>();
        if (conditionsChanged)
        {
            changed.AddRange(Alarms.Reconcile(Conditions(), now));
        }

        changed.AddRange(Alarms.Evaluate(ReadForAlarm, now));
        await CommitAlarmsAsync(changed);
    }

    private List<AlarmCondition> Conditions()
    {
        var own = Spec.TryGetProperty("alarms", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Select(declaration => declaration.GetRawText())
            : [];

        return
        [
            .. (_assetType?.Alarms ?? []).Concat(own)
            .Select(text =>
            {
                using var document = JsonDocument.Parse(text);

                return AlarmCondition.Create(document.RootElement);
            })
        ];
    }

    private JsonElement? ReadForAlarm(string dataPoint)
    {
        var point = _points.FirstOrDefault(candidate => candidate.Name == dataPoint);
        if (point is null)
        {
            return null;
        }

        var evaluated = Evaluate(point, []);
        if (evaluated.Value is null || evaluated.Quality == (int)Quality.Bad)
        {
            return null;
        }

        var value = JsonDocument.Parse(evaluated.Value).RootElement.Clone();
        var fits = point.Type switch
        {
            "bool" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "integer" or "number" => value.ValueKind == JsonValueKind.Number,
            _ => value.ValueKind == JsonValueKind.String
        };

        return fits ? value : null;
    }

    private async Task CommitAlarmsAsync(IReadOnlyList<Alarm> changed)
    {
        foreach (var alarm in changed)
        {
            _unrecorded[alarm.Id] = alarm;
        }

        ScheduleAlarmTimer();
        var pending = string.Join(
            ';',
            Alarms.Pending.Select(entry => $"{entry.Key}@{entry.Value.Since.UtcTicks}:{entry.Value.Trigger}"));
        if (_unrecorded.Count == 0 && pending == _persistedPending)
        {
            return;
        }

        await _alarmWrite.WaitAsync();
        try
        {
            alarmState.State = AlarmState.From(Alarms);
            _persistedPending = pending;
            var rows = _unrecorded.Values.ToList();
            _unrecorded.Clear();
            await alarmState.WriteStateAsync();
            try
            {
                await history.UpsertAsync(Namespace, rows);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                logger.LogWarning(exception, "Asset {Name} could not record its Alarms in the history", ResourceName);
                foreach (var row in rows)
                {
                    _unrecorded.TryAdd(row.Id, row);
                }
            }
        }
        finally
        {
            _alarmWrite.Release();
        }

        await ReportStatusAsync();
    }

    private async Task RetireAlarmsAsync()
    {
        var retired = Alarms.RetireAll(DateTimeOffset.UtcNow);
        await CommitAlarmsAsync(retired);
        _alarms = null;
        await alarmState.ClearStateAsync();
    }

    private void ScheduleAlarmTimer()
    {
        _alarmTimer?.Dispose();
        _alarmTimer = null;
        if (Alarms.NextDeadline is not { } deadline)
        {
            return;
        }

        var wait = deadline - DateTimeOffset.UtcNow;
        _alarmTimer = this.RegisterGrainTimer(
            () => SyncAlarmsAsync(false),
            wait > TimeSpan.Zero ? wait : TimeSpan.FromMilliseconds(1),
            Timeout.InfiniteTimeSpan);
    }

    private async Task SyncDevicesAsync()
    {
        var wanted = _points.Select(point => point.Source?.Device).OfType<string>().ToHashSet();
        foreach (var device in _feeds.Keys.Where(device => !wanted.Contains(device)).ToList())
        {
            await Unsubscribe(device);
        }

        foreach (var device in wanted)
        {
            try
            {
                _feeds[device] = await Device(device).SubscribeAsync(ResourceName);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                logger.LogWarning(
                    exception,
                    "Asset {Name} could not subscribe to Device {Device}",
                    ResourceName,
                    device);
            }
        }

        if (AssetTypeName is not null && _assetType is not { Exists: true })
        {
            return;
        }

        var written = values.State.Written.Keys
            .Where(name => _points.All(point => point.Name != name || point.Source is not null))
            .ToList();
        if (written.Count > 0)
        {
            foreach (var name in written)
            {
                values.State.Written.Remove(name);
            }

            await values.WriteStateAsync();
        }
    }

    private async Task Unsubscribe(string device)
    {
        _feeds.Remove(device);
        try
        {
            await Device(device).UnsubscribeAsync(ResourceName);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            logger.LogWarning(
                exception,
                "Asset {Name} could not unsubscribe from Device {Device}",
                ResourceName,
                device);
        }
    }

    private async Task ClearWrittenAsync()
    {
        if (values.State.Written.Count > 0)
        {
            values.State.Written.Clear();
            await values.WriteStateAsync();
        }
    }

    private sealed record Evaluated(string? Value, int Quality, DateTimeOffset? Timestamp)
    {
        public static Evaluated Unknown { get; } = new(null, (int)Flownodes.Sdk.Quality.Uncertain, null);
        public static Evaluated Bad { get; } = new(null, (int)Flownodes.Sdk.Quality.Bad, null);
    }
}
