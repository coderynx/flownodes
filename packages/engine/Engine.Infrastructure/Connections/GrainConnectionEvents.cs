using System.Text.Json;
using Flownodes.Sdk;

namespace Engine.Infrastructure.Connections;

internal sealed class GrainConnectionEvents(IConnectionGrain grain, long runtime) : IConnectionEvents
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    public void Connecting() => Send(grain.RuntimeConnectingAsync(runtime));

    public void Connectivity(Connectivity connectivity, string? message) =>
        Send(grain.RuntimeConnectivityAsync(runtime, (int)connectivity, message));

    public void Failed(string message) => Send(grain.RuntimeFailedAsync(runtime, message));

    public void Value(string device, string dataPoint, JsonElement value, DateTimeOffset timestamp, Quality quality) =>
        Send(grain.RuntimeValueAsync(runtime, device, dataPoint, value.GetRawText(), timestamp, (int)quality));

    public void Event(string? device, string name, JsonElement payload, DateTimeOffset timestamp) =>
        Send(grain.RuntimeEventAsync(runtime, device, name, payload.GetRawText(), timestamp));

    public void DeviceState(string device, DeviceState state) =>
        Send(grain.RuntimeDeviceStateAsync(runtime, device, (int)state));

    public void Discoveries(IReadOnlyList<Discovery> discoveries) =>
        Send(grain.RuntimeDiscoveriesAsync(runtime, JsonSerializer.Serialize(discoveries, Web)));

    private static void Send(Task call) => call.Ignore();
}
