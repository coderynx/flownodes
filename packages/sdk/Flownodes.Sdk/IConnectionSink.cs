using System.Text.Json;

namespace Flownodes.Sdk;

/// <summary>
/// Whether a Connection is working, as the Integration sees it.
/// </summary>
public enum Connectivity
{
    /// <summary>
    /// Fully working.
    /// </summary>
    Connected,

    /// <summary>
    /// Working in part, such as reaching only some of its Devices.
    /// </summary>
    Degraded
}

/// <summary>
/// Whether a declared Device was found on its Connection.
/// </summary>
public enum DeviceState
{
    /// <summary>
    /// The Device answers.
    /// </summary>
    Found,

    /// <summary>
    /// Nothing is at the Device's address.
    /// </summary>
    NotFound,

    /// <summary>
    /// The Device is there but cannot be reached.
    /// </summary>
    Unreachable
}

/// <summary>
/// Something a Connection can reach that nobody has declared as a Device.
/// </summary>
/// <param name="DeviceType">The name of the Device Type it would be.</param>
/// <param name="Address">The address a Device Spec would give.</param>
/// <param name="SuggestedName">A name for the Device Resource.</param>
/// <param name="SuggestedConfig">A Device configuration to pre-fill, if any.</param>
public sealed record Discovery(
    string DeviceType,
    JsonElement Address,
    string SuggestedName,
    JsonElement? SuggestedConfig = null);

/// <summary>
/// Where a Connection pushes everything it learns; it is safe to call from any thread.
/// </summary>
public interface IConnectionSink
{
    /// <summary>
    /// Reports whether the Connection is connected or degraded.
    /// </summary>
    /// <param name="connectivity">The connectivity.</param>
    /// <param name="message">An explanation for Engineers, if any.</param>
    void ReportConnectivity(Connectivity connectivity, string? message = null);

    /// <summary>
    /// Reports that the Connection can no longer continue. The host disposes it and starts a new one with capped
    /// backoff.
    /// </summary>
    /// <param name="message">Why, for Engineers.</param>
    void ReportFailure(string message);

    /// <summary>
    /// Reports a Data Point value.
    /// </summary>
    /// <param name="device">The name of the Device.</param>
    /// <param name="dataPoint">The name of the Data Point.</param>
    /// <param name="value">The value as JSON.</param>
    /// <param name="sourceTimestamp">When the device produced it.</param>
    /// <param name="quality">Its Quality.</param>
    void ReportValue(
        string device,
        string dataPoint,
        JsonElement value,
        DateTimeOffset sourceTimestamp,
        Quality quality = Quality.Good);

    /// <summary>
    /// Reports whether a declared Device was found.
    /// </summary>
    /// <param name="device">The name of the Device.</param>
    /// <param name="state">The state.</param>
    void ReportDeviceState(string device, DeviceState state);

    /// <summary>
    /// Reports everything the Connection can currently reach as a full snapshot. The host drops what is already
    /// declared as a Device.
    /// </summary>
    /// <param name="discoveries">Every unit reachable.</param>
    void ReportDiscoveries(IReadOnlyList<Discovery> discoveries);
}
