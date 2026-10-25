using System.Text.Json;
using Flownodes.Sdk;

namespace Engine.Infrastructure.Connections;

internal interface IConnectionEvents
{
    void Connecting();
    void Connectivity(Connectivity connectivity, string? message);
    void Failed(string message);
    void Value(string device, string dataPoint, JsonElement value, DateTimeOffset timestamp, Quality quality);
    void DeviceState(string device, DeviceState state);
    void Discoveries(IReadOnlyList<Discovery> discoveries);
}
