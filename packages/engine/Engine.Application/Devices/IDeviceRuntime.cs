using System.Text.Json;
using Coderynx.Functional.Results;

namespace Engine.Application.Devices;

/// <summary>
/// One Data Point of a Device as it is now: what the Device Type derived for it and the latest value the Connection
/// reported.
/// </summary>
/// <param name="Name">The Data Point's name.</param>
/// <param name="Type">
/// The type of its value: <c>bool</c>, <c>integer</c>, <c>number</c>, <c>string</c> or <c>enum</c>.
/// </param>
/// <param name="Writable">Whether it can be written.</param>
/// <param name="Unit">The unit of a numeric value, if any.</param>
/// <param name="Min">The smallest value it can hold, if bounded.</param>
/// <param name="Max">The largest value it can hold, if bounded.</param>
/// <param name="Values">The allowed texts of an enum Data Point.</param>
/// <param name="Value">The latest value, or <see langword="null"/> before one has been reported.</param>
/// <param name="Quality">Its Quality: <c>good</c>, <c>uncertain</c> or <c>bad</c>.</param>
/// <param name="SourceTimestamp">When the device produced the value, if one has been reported.</param>
public sealed record DataPointReading(
    string Name,
    string Type,
    bool Writable,
    string? Unit,
    double? Min,
    double? Max,
    IReadOnlyList<string>? Values,
    JsonElement? Value,
    string Quality,
    DateTimeOffset? SourceTimestamp);

/// <summary>
/// The live side of Devices: what the Engine holds in memory and what it asks of the devices.
/// </summary>
public interface IDeviceRuntime
{
    /// <summary>
    /// Reads every Data Point of a declared Device.
    /// </summary>
    /// <param name="namespace">The Namespace of the Device.</param>
    /// <param name="device">The name of the Device.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The Data Points, or a not-found error when the Engine knows no such Device.</returns>
    Task<Result<IReadOnlyList<DataPointReading>>> ListDataPointsAsync(
        string @namespace,
        string device,
        CancellationToken cancellationToken);

    /// <summary>
    /// Writes a writable Data Point on the device, completing when the device has acknowledged. The Data Point's value
    /// changes only when the Connection reports it.
    /// </summary>
    /// <param name="namespace">The Namespace of the Device.</param>
    /// <param name="device">The name of the Device.</param>
    /// <param name="dataPoint">The name of the Data Point.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or the reason the write was refused or failed.</returns>
    Task<Result> WriteDataPointAsync(
        string @namespace,
        string device,
        string dataPoint,
        JsonElement value,
        CancellationToken cancellationToken);

    /// <summary>
    /// Calls an Action on the device.
    /// </summary>
    /// <param name="namespace">The Namespace of the Device.</param>
    /// <param name="device">The name of the Device.</param>
    /// <param name="action">The name of the Action.</param>
    /// <param name="parameters">The parameters as JSON.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The Action's result, or the reason the call was refused or failed.</returns>
    Task<Result<JsonElement>> InvokeActionAsync(
        string @namespace,
        string device,
        string action,
        JsonElement parameters,
        CancellationToken cancellationToken);
}
