using System.Text.Json;
using Engine.Infrastructure.Assets;
using Engine.Infrastructure.Connections;
using Engine.Infrastructure.DataPoints;
using Engine.Infrastructure.Flows;
using Engine.Infrastructure.Integrations;
using Engine.Infrastructure.Resources;
using Flownodes.Sdk;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace Engine.Infrastructure.Devices;

[Reentrant]
internal sealed class DeviceGrain(
    [PersistentState("device")] IPersistentState<ResourceState> state,
    IStatusReporter reporter,
    IIntegrationCatalog integrations,
    IOptions<DeviceGrainOptions> options,
    ILogger<DeviceGrain> logger) : ResourceGrain(state, reporter, options.Value.StatusInterval, "Device"), IDeviceGrain
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string, Reading> _readings = [];
    private ConnectionInfo? _connection;
    private DeviceSurface? _surface;
    private string? _problem;
    private DeviceState? _deviceState;
    private readonly HashSet<string> _subscribers = [];
    private readonly Dictionary<IFlowWatcher, HashSet<string>> _watchers = [];
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
        var previousConnection = _registeredWith;
        if (!await AcceptAsync(uid, generation, seq, spec))
        {
            return;
        }

        _readings.Clear();
        _deviceState = null;
        _surface = null;
        if (previousConnection is not null && previousConnection != ConnectionName)
        {
            await Connection(previousConnection).UnregisterDeviceAsync(ResourceName);
        }

        await RegisterAsync();
        await ReportStatusAsync();
        Notify();
    }

    public async Task DeleteAsync(Guid uid, long seq)
    {
        var connection = _registeredWith;
        if (!await TombstoneAsync(uid, seq))
        {
            return;
        }

        if (connection is not null)
        {
            await Connection(connection).UnregisterDeviceAsync(ResourceName);
        }

        Notify();
        DeactivateOnIdle();
    }

    public async Task PingAsync()
    {
        if (IsLive)
        {
            await RegisterAsync();
        }
    }

    public async Task ConnectionChangedAsync(ConnectionInfo connection)
    {
        if (!IsLive)
        {
            return;
        }

        Adopt(connection);
        await ReportStatusAsync();
        Notify();
    }

    public Task<DeviceFeed> SubscribeAsync(string asset)
    {
        _subscribers.Add(asset);

        return Task.FromResult(Feed());
    }

    public Task UnsubscribeAsync(string asset)
    {
        _subscribers.Remove(asset);

        return Task.CompletedTask;
    }

    public Task ValueReportedAsync(string dataPoint, string value, DateTimeOffset timestamp, int quality)
    {
        var isNewer = !_readings.TryGetValue(dataPoint, out var known) || timestamp >= known.Timestamp;
        if (IsLive && isNewer)
        {
            _readings[dataPoint] = new Reading(value, (Quality)quality, timestamp);
            Notify();
            if (known?.Value != value)
            {
                NotifyWatchers(DataPointKey(dataPoint));
            }
        }

        return Task.CompletedTask;
    }

    public Task EventReportedAsync(string name, string payload, DateTimeOffset timestamp)
    {
        var declared = _surface?.Events.Any(candidate => candidate.Name == name) == true;
        if (!IsLive || !declared)
        {
            return Task.CompletedTask;
        }

        foreach (var watcher in Watching(EventKey(name)))
        {
            watcher.EventReportedAsync(ResourceName, name, payload, timestamp).Ignore();
        }

        return Task.CompletedTask;
    }

    public Task<WatchResult> WatchDataPointAsync(IFlowWatcher watcher, string dataPoint)
    {
        var missing = _surface is { } surface && surface.DataPoints.All(candidate => candidate.Name != dataPoint);
        if (!IsLive || missing)
        {
            return Task.FromResult(WatchResult.Missing);
        }

        Watch(watcher, DataPointKey(dataPoint));

        return Task.FromResult(new WatchResult(true, Snapshots().FirstOrDefault(point => point.Name == dataPoint)));
    }

    public Task<bool> WatchEventAsync(IFlowWatcher watcher, string name)
    {
        var missing = _surface is { } surface && surface.Events.All(candidate => candidate.Name != name);
        if (!IsLive || missing)
        {
            return Task.FromResult(false);
        }

        Watch(watcher, EventKey(name));

        return Task.FromResult(true);
    }

    public Task UnwatchAsync(IFlowWatcher watcher)
    {
        _watchers.Remove(watcher);

        return Task.CompletedTask;
    }

    public async Task StateReportedAsync(int deviceState)
    {
        if (IsLive && _deviceState != (DeviceState)deviceState)
        {
            _deviceState = (DeviceState)deviceState;
            await ReportStatusAsync();
        }
    }

    public Task<IReadOnlyList<DataPointSnapshot>?> GetDataPointsAsync() =>
        Task.FromResult<IReadOnlyList<DataPointSnapshot>?>(IsLive ? Snapshots() : null);

    public async Task<CallResult> WriteAsync(string dataPoint, string value)
    {
        var definition = _surface?.DataPoints.FirstOrDefault(candidate => candidate.Name == dataPoint);
        if (!IsLive || definition is null)
        {
            return CallResult.Failure("NotFound", $"The Device has no Data Point '{dataPoint}'.");
        }

        if (!definition.Writable)
        {
            return CallResult.Failure("ReadOnly", $"Data Point '{dataPoint}' is read-only.");
        }

        if (Check(definition, value) is { } problem)
        {
            return CallResult.Failure("InvalidValue", problem);
        }

        return await Connection(ConnectionName).WriteAsync(ResourceName, dataPoint, value);
    }

    public async Task<CallResult> InvokeAsync(string action, string parameters, string invocationId)
    {
        if (!IsLive || _surface?.Actions.Any(candidate => candidate.Name == action) != true)
        {
            return CallResult.Failure("NotFound", $"The Device has no Action '{action}'.");
        }

        return await Connection(ConnectionName).InvokeAsync(ResourceName, action, parameters, invocationId);
    }

    protected override JsonElement BuildStatus()
    {
        var (reason, ready, message) = Condition();

        return JsonSerializer.SerializeToElement(
            new
            {
                conditions = new[]
                {
                    new
                    {
                        type = "Ready",
                        status = ready ? "True" : "False",
                        reason,
                        message
                    }
                },
                state = _deviceState?.ToString() ?? "Unknown"
            },
            Web);
    }

    private DeviceFeed Feed() => new(IsLive, IsLive && _surface is not null, IsLive ? Snapshots() : []);

    private void Notify()
    {
        if (_subscribers.Count == 0)
        {
            return;
        }

        var feed = Feed();
        foreach (var asset in _subscribers)
        {
            GrainFactory.GetGrain<IAssetGrain>($"{Namespace}/{asset}").DeviceChangedAsync(ResourceName, feed).Ignore();
        }
    }

    private static string DataPointKey(string name) => $"dataPoint:{name}";
    private static string EventKey(string name) => $"event:{name}";

    private void Watch(IFlowWatcher watcher, string key)
    {
        if (!_watchers.TryGetValue(watcher, out var keys))
        {
            _watchers[watcher] = keys = [];
        }

        keys.Add(key);
    }

    private IEnumerable<IFlowWatcher> Watching(string key) =>
        [.. _watchers.Where(entry => entry.Value.Contains(key)).Select(entry => entry.Key)];

    private void NotifyWatchers(string key)
    {
        var snapshot = Snapshots().FirstOrDefault(point => DataPointKey(point.Name) == key);
        if (snapshot is null)
        {
            return;
        }

        foreach (var watcher in Watching(key))
        {
            watcher.DataPointChangedAsync("Device", ResourceName, snapshot).Ignore();
        }
    }

    private List<DataPointSnapshot> Snapshots()
    {
        var up = _connection?.IsUp == true;

        return
        [
            .. (_surface?.DataPoints ?? []).Select(definition =>
            {
                var reading = _readings.GetValueOrDefault(definition.Name);

                return new DataPointSnapshot(
                    definition.Name,
                    definition.Type.ToString().ToLowerInvariant(),
                    definition.Writable,
                    definition.Unit,
                    definition.Min,
                    definition.Max,
                    definition.Values,
                    reading?.Value,
                    up && reading is not null ? (int)reading.Quality : (int)Quality.Bad,
                    reading?.Timestamp);
            })
        ];
    }

    private static string? Check(DataPointDefinition definition, string value)
    {
        using var document = JsonDocument.Parse(value);

        return ValueCheck.Problem(
            definition.Type.ToString().ToLowerInvariant(),
            definition.Min,
            definition.Max,
            definition.Values,
            document.RootElement);
    }

    private string ConnectionName => Spec.GetProperty("connection").GetString()!;

    private IConnectionGrain Connection(string name) =>
        GrainFactory.GetGrain<IConnectionGrain>($"{Namespace}/{name}");

    private (string Reason, bool Ready, string? Message) Condition()
    {
        if (_problem is not null)
        {
            return ("DeviceTypeMissing", false, _problem);
        }

        if (_connection is not { IsUp: true })
        {
            return ("ConnectionNotReady", false, $"Connection '{ConnectionName}' is not connected.");
        }

        return _deviceState switch
        {
            DeviceState.Found => ("Found", true, null),
            DeviceState.NotFound => ("NotFound", false, "The Connection did not find the Device at its address."),
            DeviceState.Unreachable => ("Unreachable", false, "The Device cannot be reached."),
            _ => ("Waiting", false, "The Connection has not yet reported on the Device.")
        };
    }

    private async Task RegisterAsync()
    {
        try
        {
            var spec = Spec;
            var registration = new DeviceRegistration(
                ResourceName,
                spec.GetProperty("type").GetString()!,
                spec.GetProperty("address").GetRawText(),
                spec.TryGetProperty("config", out var config) ? config.GetRawText() : "{}");
            _registeredWith = ConnectionName;
            Adopt(await Connection(ConnectionName).RegisterDeviceAsync(registration));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            logger.LogWarning(exception, "Device {Name} could not register with its Connection", ResourceName);
        }
    }

    private void Adopt(ConnectionInfo connection)
    {
        _connection = connection;
        DeriveSurface();
        if (!connection.IsUp)
        {
            foreach (var (name, reading) in _readings.ToList())
            {
                _readings[name] = reading with { Quality = Quality.Bad };
            }
        }
    }

    private void DeriveSurface()
    {
        _problem = null;
        if (_connection?.Integration is not { } integration || integrations.Find(integration) is not { } loaded)
        {
            return;
        }

        var spec = Spec;
        var typeName = spec.GetProperty("type").GetString();
        var deviceType = loaded.Integration.DeviceTypes.FirstOrDefault(candidate => candidate.Name == typeName);
        if (deviceType is null)
        {
            _surface = null;
            _problem = $"Integration '{integration}' has no Device Type '{typeName}'.";

            return;
        }

        var config = spec.TryGetProperty("config", out var given) ? given : JsonDocument.Parse("{}").RootElement;
        _surface = deviceType.Surface(spec.GetProperty("address"), config);
    }

    private sealed record Reading(string Value, Quality Quality, DateTimeOffset Timestamp);
}
