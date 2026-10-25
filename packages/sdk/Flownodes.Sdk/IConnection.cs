using System.Text.Json;

namespace Flownodes.Sdk;

/// <summary>
/// A declared Device as a Connection is told about it.
/// </summary>
/// <param name="Name">The Device's name, unique within its Namespace.</param>
/// <param name="Type">The name of its Device Type.</param>
/// <param name="Address">Its address, as validated against the Device Type.</param>
/// <param name="Config">Its configuration; an empty object when it has none.</param>
public sealed record DeviceBinding(string Name, string Type, JsonElement Address, JsonElement Config);

/// <summary>
/// A running Connection to the outside world, created by <see cref="IIntegration.CreateConnection"/>.
/// </summary>
/// <remarks>
/// The host calls it off the Orleans scheduler, from one Silo at a time, and restarts it with backoff when it throws.
/// Everything it learns flows out through the <see cref="IConnectionSink"/> it was created with.
/// </remarks>
public interface IConnection : IAsyncDisposable
{
    /// <summary>
    /// Connects, and keeps pushing values and connectivity through the sink until disposed.
    /// </summary>
    /// <param name="cancellationToken">Cancels the connect.</param>
    /// <returns>A task that completes once the Connection is up.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Tells the Connection the whole set of Devices declared on it, now and whenever it changes.
    /// </summary>
    /// <param name="devices">Every declared Device of the Connection.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that completes when the Connection has taken the set.</returns>
    Task SetDevicesAsync(IReadOnlyList<DeviceBinding> devices, CancellationToken cancellationToken);

    /// <summary>
    /// Writes a Data Point on a Device, completing on the device's acknowledgement. The Data Point's value changes
    /// only when the Connection reports it through the sink.
    /// </summary>
    /// <param name="device">The name of the Device.</param>
    /// <param name="dataPoint">The name of the Data Point.</param>
    /// <param name="value">The value, already checked against the Data Point's type.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that completes when the device has acknowledged the write.</returns>
    Task WriteAsync(string device, string dataPoint, JsonElement value, CancellationToken cancellationToken);

    /// <summary>
    /// Invokes an Action on a Device.
    /// </summary>
    /// <param name="device">The name of the Device.</param>
    /// <param name="action">The name of the Action.</param>
    /// <param name="parameters">The parameters as JSON.</param>
    /// <param name="invocationId">Identifies the call, so a repeat can be recognised.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The result as JSON.</returns>
    Task<JsonElement> InvokeAsync(
        string device,
        string action,
        JsonElement parameters,
        string invocationId,
        CancellationToken cancellationToken);
}
