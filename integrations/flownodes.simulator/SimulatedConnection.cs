using System.Collections.Concurrent;
using System.Text.Json;
using Flownodes.Sdk;

namespace Flownodes.Simulator;

internal sealed class SimulatedConnection(TimeProvider time, int units, IConnectionSink sink) : IConnection
{
    public const string Thermometer = "Thermometer";
    public const string Switch = "Switch";
    private const double InitialTemperature = 20;
    private readonly ConcurrentDictionary<string, DeviceBinding> _devices = new();
    private readonly ConcurrentDictionary<string, bool> _switches = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        sink.ReportConnectivity(Connectivity.Connected);
        sink.ReportDiscoveries(
        [
            .. Enumerable.Range(1, units)
                .Select(id => new Discovery(
                    Thermometer,
                    JsonSerializer.SerializeToElement(new { id }),
                    $"thermometer-{id}"))
        ]);

        return Task.CompletedTask;
    }

    public Task SetDevicesAsync(IReadOnlyList<DeviceBinding> devices, CancellationToken cancellationToken)
    {
        _devices.Clear();
        foreach (var device in devices)
        {
            _devices[device.Name] = device;
            var found = Id(device) is > 0 && Id(device) <= units;
            sink.ReportDeviceState(device.Name, found ? DeviceState.Found : DeviceState.NotFound);
            if (found)
            {
                Publish(device);
            }
        }

        return Task.CompletedTask;
    }

    public Task WriteAsync(string device, string dataPoint, JsonElement value, CancellationToken cancellationToken)
    {
        _switches[device] = value.GetBoolean();
        Report(device, "on", JsonSerializer.SerializeToElement(value.GetBoolean()));

        return Task.CompletedTask;
    }

    public Task<JsonElement> InvokeAsync(
        string device,
        string action,
        JsonElement parameters,
        string invocationId,
        CancellationToken cancellationToken)
    {
        switch (action)
        {
            case "set":
                var temperature = parameters.GetProperty("value").GetDouble();
                Report(device, "temperature", JsonSerializer.SerializeToElement(temperature));

                return Task.FromResult(JsonSerializer.SerializeToElement(new { temperature }));
            case "toggle":
                var on = !_switches.GetValueOrDefault(device);
                _switches[device] = on;
                Report(device, "on", JsonSerializer.SerializeToElement(on));

                return Task.FromResult(JsonSerializer.SerializeToElement(new { on }));
            default:
                sink.ReportFailure("The simulated connection was told to fail.");

                return Task.FromResult(JsonSerializer.SerializeToElement(new { }));
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    private static int Id(DeviceBinding device) => device.Address.GetProperty("id").GetInt32();

    private void Publish(DeviceBinding device)
    {
        if (device.Type == Thermometer)
        {
            Report(device.Name, "temperature", JsonSerializer.SerializeToElement(InitialTemperature));
        }
        else
        {
            Report(device.Name, "on", JsonSerializer.SerializeToElement(_switches.GetValueOrDefault(device.Name)));
        }
    }

    private void Report(string device, string dataPoint, JsonElement value) =>
        sink.ReportValue(device, dataPoint, value, time.GetUtcNow());
}
