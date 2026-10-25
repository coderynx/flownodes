using System.Text.Json;
using Engine.Infrastructure.Connections;
using Engine.Infrastructure.DataPoints;
using Engine.Infrastructure.Devices;
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
    IStatusReporter reporter,
    IOptions<AssetGrainOptions> options,
    ILogger<AssetGrain> logger) : ResourceGrain(state, reporter, options.Value.StatusInterval, "Asset"), IAssetGrain
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string, DeviceFeed> _feeds = [];
    private IReadOnlyList<AssetDataPoint> _points = [];
    private AssetTypeInfo? _assetType;
    private string? _registeredWith;

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
        await ReportStatusAsync();
    }

    public Task DeviceChangedAsync(string device, DeviceFeed feed)
    {
        if (IsLive && _feeds.ContainsKey(device))
        {
            _feeds[device] = feed;
        }

        return ReportStatusAsync();
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

        return CallResult.Success();
    }

    protected override JsonElement BuildStatus()
    {
        var (reason, ready, message) = Condition();

        return JsonSerializer.SerializeToElement(
            new
            {
                conditions = new[] { new { type = "Ready", status = ready ? "True" : "False", reason, message } },
                dataPoints = ready ? _points.Select(point => point.Name).ToArray() : []
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

    private void Refresh() => _points = AssetDataPoint.Merge(_assetType?.DataPoints ?? [], Spec);

    private async Task RegisterAsync()
    {
        _registeredWith = AssetTypeName;
        _assetType = AssetTypeName is { } name ? await AssetType(name).RegisterInstanceAsync(ResourceName) : null;
        Refresh();
        await SyncDevicesAsync();
        await ReportStatusAsync();
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
