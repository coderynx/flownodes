using System.Text.Json;
using Engine.Infrastructure.Connections;
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
    }

    public Task ValueReportedAsync(string dataPoint, string value, DateTimeOffset timestamp, int quality)
    {
        var isNewer = !_readings.TryGetValue(dataPoint, out var known) || timestamp >= known.Timestamp;
        if (IsLive && isNewer)
        {
            _readings[dataPoint] = new Reading(value, (Quality)quality, timestamp);
        }

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

    public Task<IReadOnlyList<DataPointSnapshot>?> GetDataPointsAsync()
    {
        if (!IsLive)
        {
            return Task.FromResult<IReadOnlyList<DataPointSnapshot>?>(null);
        }

        var up = _connection?.IsUp == true;
        IReadOnlyList<DataPointSnapshot> snapshots =
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

        return Task.FromResult<IReadOnlyList<DataPointSnapshot>?>(snapshots);
    }

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

    private static string? Check(DataPointDefinition definition, string value)
    {
        using var document = JsonDocument.Parse(value);
        var json = document.RootElement;
        var type = definition.Type;
        var fits = type switch
        {
            DataPointType.Bool => json.ValueKind is JsonValueKind.True or JsonValueKind.False,
            DataPointType.Integer => json.ValueKind == JsonValueKind.Number && json.TryGetInt64(out _),
            DataPointType.Number => json.ValueKind == JsonValueKind.Number,
            DataPointType.String => json.ValueKind == JsonValueKind.String,
            _ => json.ValueKind == JsonValueKind.String && definition.Values?.Contains(json.GetString()!) == true
        };
        if (!fits)
        {
            return $"Must be a {type.ToString().ToLowerInvariant()}.";
        }

        if (json.ValueKind == JsonValueKind.Number)
        {
            var number = json.GetDouble();
            if (number < definition.Min || number > definition.Max)
            {
                return $"Must be between {definition.Min} and {definition.Max}.";
            }
        }

        return null;
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
