using System.Text.Json;
using Engine.Infrastructure.Devices;
using Engine.Infrastructure.Flows;
using Engine.Infrastructure.Integrations;
using Engine.Infrastructure.Resources;
using Flownodes.Sdk;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace Engine.Infrastructure.Connections;

[Reentrant]
internal sealed class ConnectionGrain(
    [PersistentState("connection")] IPersistentState<ResourceState> state,
    IStatusReporter reporter,
    IIntegrationCatalog integrations,
    IOptions<ConnectionGrainOptions> options,
    ILogger<ConnectionGrain> logger)
    : ResourceGrain(state, reporter, options.Value.StatusInterval, "Connection"), IConnectionGrain
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string, DeviceRegistration> _devices = [];
    private readonly Dictionary<string, int> _deviceStates = [];
    private ConnectionRuntime? _runtime;
    private long _runtimeId;
    private ConnectionPhase _phase = ConnectionPhase.Unknown;
    private string? _message;
    private string _discoveries = "[]";
    private string? _integration;
    private ConnectionSurface? _surface;
    private readonly Dictionary<IFlowWatcher, HashSet<string>> _watchers = [];

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (IsLive)
        {
            await StartAsync();
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken) =>
        await StopAsync();

    public async Task ApplyAsync(Guid uid, long generation, long seq, string spec)
    {
        var previous = State.Spec;
        var sameIncarnation = State.Uid == uid;
        if (!await AcceptAsync(uid, generation, seq, spec))
        {
            return;
        }

        var changed = !sameIncarnation || !SpecEquals(previous, spec);
        if (changed || IsIdle)
        {
            await StopAsync();
            await StartAsync();
        }

        await ReportStatusAsync();
    }

    public async Task DeleteAsync(Guid uid, long seq)
    {
        if (!await TombstoneAsync(uid, seq))
        {
            return;
        }

        await StopAsync();
        await NotifyDevicesAsync();
        DeactivateOnIdle();
    }

    public async Task PingAsync()
    {
        if (IsLive && IsIdle)
        {
            await StartAsync();
        }
    }

    public async Task<ConnectionInfo> RegisterDeviceAsync(DeviceRegistration device)
    {
        var changed = !_devices.TryGetValue(device.Name, out var known) || known != device;
        _devices[device.Name] = device;
        if (changed)
        {
            await PushDevicesAsync();
            await ReportStatusAsync();
        }

        if (_deviceStates.TryGetValue(device.Name, out var deviceState))
        {
            Grain(device.Name).StateReportedAsync(deviceState).Ignore();
        }

        return Info();
    }

    public async Task UnregisterDeviceAsync(string name)
    {
        if (_devices.Remove(name))
        {
            _deviceStates.Remove(name);
            await PushDevicesAsync();
            await ReportStatusAsync();
        }
    }

    public async Task<CallResult> WriteAsync(string device, string dataPoint, string value)
    {
        if (_runtime is null || !Info().IsUp)
        {
            return CallResult.Failure("ConnectionNotReady", "The Connection is not connected.");
        }

        using var timeout = new CancellationTokenSource(options.Value.CallTimeout);
        try
        {
            using var document = JsonDocument.Parse(value);
            await _runtime.WriteAsync(device, dataPoint, document.RootElement.Clone(), timeout.Token);

            return CallResult.Success();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return Fail(exception, "write");
        }
    }

    public async Task<CallResult> InvokeAsync(string? device, string action, string parameters, string invocationId)
    {
        var offered = IsLive && _surface?.Actions.Any(candidate => candidate.Name == action) == true;
        if (device is null && !offered)
        {
            return CallResult.Failure("NotFound", $"The Connection has no Action '{action}'.");
        }

        if (_runtime is null || !Info().IsUp)
        {
            return CallResult.Failure("ConnectionNotReady", "The Connection is not connected.");
        }

        using var timeout = new CancellationTokenSource(options.Value.CallTimeout);
        try
        {
            using var document = JsonDocument.Parse(parameters);
            var result = await _runtime.InvokeAsync(
                device,
                action,
                document.RootElement.Clone(),
                invocationId,
                timeout.Token);

            return CallResult.Success(result.GetRawText());
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return Fail(exception, "action");
        }
    }

    public Task<bool> WatchEventAsync(IFlowWatcher watcher, string name)
    {
        if (!IsLive || _surface?.Events.Any(candidate => candidate.Name == name) != true)
        {
            return Task.FromResult(false);
        }

        if (!_watchers.TryGetValue(watcher, out var names))
        {
            _watchers[watcher] = names = [];
        }

        names.Add(name);

        return Task.FromResult(true);
    }

    public Task UnwatchAsync(IFlowWatcher watcher)
    {
        _watchers.Remove(watcher);

        return Task.CompletedTask;
    }

    public Task RuntimeConnectingAsync(long runtime) => OnRuntimeAsync(runtime, ConnectionPhase.Connecting, null);

    public Task RuntimeConnectivityAsync(long runtime, int connectivity, string? message) =>
        OnRuntimeAsync(
            runtime,
            (Connectivity)connectivity == Connectivity.Connected ? ConnectionPhase.Connected : ConnectionPhase.Degraded,
            message);

    public Task RuntimeFailedAsync(long runtime, string message) =>
        OnRuntimeAsync(runtime, ConnectionPhase.Failed, message);

    public Task RuntimeValueAsync(
        long runtime,
        string device,
        string dataPoint,
        string value,
        DateTimeOffset timestamp,
        int quality)
    {
        if (runtime == _runtimeId && _devices.ContainsKey(device))
        {
            return Grain(device).ValueReportedAsync(dataPoint, value, timestamp, quality);
        }

        return Task.CompletedTask;
    }

    public Task RuntimeEventAsync(long runtime, string? device, string name, string payload, DateTimeOffset timestamp)
    {
        if (runtime != _runtimeId)
        {
            return Task.CompletedTask;
        }

        if (device is not null)
        {
            return _devices.ContainsKey(device)
                ? Grain(device).EventReportedAsync(name, payload, timestamp)
                : Task.CompletedTask;
        }

        if (_surface?.Events.Any(candidate => candidate.Name == name) == true)
        {
            foreach (var watcher in _watchers.Where(entry => entry.Value.Contains(name)).Select(entry => entry.Key))
            {
                watcher.EventReportedAsync("Connection", ResourceName, name, payload, timestamp).Ignore();
            }
        }

        return Task.CompletedTask;
    }

    public Task RuntimeDeviceStateAsync(long runtime, string device, int deviceState)
    {
        if (runtime != _runtimeId)
        {
            return Task.CompletedTask;
        }

        _deviceStates[device] = deviceState;

        return _devices.ContainsKey(device) ? Grain(device).StateReportedAsync(deviceState) : Task.CompletedTask;
    }

    public async Task RuntimeDiscoveriesAsync(long runtime, string discoveries)
    {
        if (runtime == _runtimeId && discoveries != _discoveries)
        {
            _discoveries = discoveries;
            await ReportStatusAsync();
        }
    }

    protected override JsonElement BuildStatus()
    {
        var (reason, ready) = _phase switch
        {
            ConnectionPhase.Connected => ("Connected", true),
            ConnectionPhase.Degraded => ("Degraded", false),
            ConnectionPhase.Failed => ("Failed", false),
            ConnectionPhase.IntegrationMissing => ("IntegrationMissing", false),
            ConnectionPhase.ConfigInvalid => ("ConfigInvalid", false),
            _ => ("Connecting", false)
        };

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
                        message = _message
                    }
                },
                connectivity = _phase == ConnectionPhase.Unknown ? "Connecting" : _phase.ToString(),
                discoveries = UndeclaredDiscoveries(),
                actions = (_surface?.Actions ?? []).Select(action => new
                {
                    name = action.Name,
                    safeToRepeat = action.SafeToRepeat,
                    parameters = action.Parameters,
                    result = action.Result
                }),
                events = (_surface?.Events ?? []).Select(@event => new { name = @event.Name, payload = @event.Payload })
            },
            Web);
    }

    private bool IsIdle => _runtime is null && _phase == ConnectionPhase.Unknown;

    private static bool SpecEquals(string? a, string b) =>
        a is not null && JsonElement.DeepEquals(JsonDocument.Parse(a).RootElement, JsonDocument.Parse(b).RootElement);

    private IDeviceGrain Grain(string device) => GrainFactory.GetGrain<IDeviceGrain>($"{Namespace}/{device}");
    private ConnectionInfo Info() => new(_integration, _phase);

    private CallResult Fail(Exception exception, string what)
    {
        logger.LogWarning(exception, "A {What} on Connection {Name} failed", what, ResourceName);

        return exception is OperationCanceledException
            ? CallResult.Failure("Timeout", $"The {what} timed out.")
            : CallResult.Failure("Failed", exception.Message);
    }

    private IEnumerable<JsonElement> UndeclaredDiscoveries()
    {
        var declared = _devices.Values.Select(device => (device.Type, Address: Parse(device.Address))).ToList();

        return JsonDocument.Parse(_discoveries)
            .RootElement.EnumerateArray()
            .Where(found => !declared.Exists(device =>
                device.Type == found.GetProperty("deviceType").GetString() && JsonElement.DeepEquals(
                    device.Address,
                    found.GetProperty("address"))))
            .Select(found => found.Clone());
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private async Task OnRuntimeAsync(long runtime, ConnectionPhase phase, string? message)
    {
        if (runtime != _runtimeId)
        {
            return;
        }

        var changed = _phase != phase;
        _phase = phase;
        _message = message;
        if (changed)
        {
            await NotifyDevicesAsync();
        }

        await ReportStatusAsync();
    }

    private async Task StartAsync()
    {
        var spec = Spec;
        _integration = spec.GetProperty("integration").GetString()!;
        _message = null;
        _surface = null;
        var config = spec.TryGetProperty("config", out var given) ? given : Parse("{}");
        var loaded = integrations.Find(_integration);
        if (loaded is null)
        {
            _phase = ConnectionPhase.IntegrationMissing;
            _message = $"Integration '{_integration}' is not installed.";
        }
        else if (loaded.Integration.ConnectionConfig.Validate(config) is { Count: > 0 } problems)
        {
            _phase = ConnectionPhase.ConfigInvalid;
            _message = string.Join(' ', problems.Select(problem => $"{problem.Path}: {problem.Message}"));
        }
        else
        {
            _surface = loaded.Integration.Surface(config);
            _runtimeId++;
            _phase = ConnectionPhase.Connecting;
            _runtime = new ConnectionRuntime(
                loaded,
                config,
                new GrainConnectionEvents(this.AsReference<IConnectionGrain>(), _runtimeId),
                options.Value);
            await _runtime.SetDevicesAsync(Bindings());
            _runtime.Start();
        }

        await NotifyDevicesAsync();
    }

    private async Task StopAsync()
    {
        _runtimeId++;
        if (_runtime is { } runtime)
        {
            _runtime = null;
            await runtime.DisposeAsync();
        }

        _phase = ConnectionPhase.Unknown;
    }

    private Task PushDevicesAsync() => _runtime?.SetDevicesAsync(Bindings()) ?? Task.CompletedTask;

    private List<DeviceBinding> Bindings() =>
    [
        .. _devices.Values.Select(device => new DeviceBinding(
            device.Name,
            device.Type,
            Parse(device.Address),
            Parse(device.Config)))
    ];

    private async Task NotifyDevicesAsync()
    {
        var info = Info();
        foreach (var device in _devices.Keys.ToList())
        {
            await Grain(device).ConnectionChangedAsync(info);
        }
    }
}
