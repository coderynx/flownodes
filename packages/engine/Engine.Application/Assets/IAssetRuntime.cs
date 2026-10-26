using System.Text.Json;
using Coderynx.Functional.Results;
using Engine.Application.Devices;

namespace Engine.Application.Assets;

/// <summary>
/// The live side of Assets: the values of their Data Points, which the Engine holds in memory, and what it asks of the
/// Devices behind them.
/// </summary>
public interface IAssetRuntime
{
    /// <summary>
    /// Reads every Data Point of a declared Asset, its Asset Type's included.
    /// </summary>
    /// <param name="namespace">The Namespace of the Asset.</param>
    /// <param name="asset">The name of the Asset.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The Data Points, or a not-found error when the Engine knows no such Asset.</returns>
    Task<Result<IReadOnlyList<DataPointReading>>> ListDataPointsAsync(
        string @namespace,
        string asset,
        CancellationToken cancellationToken);

    /// <summary>
    /// Writes a writable Data Point of an Asset. One mapped to a writable Device Data Point is written through to the
    /// Device, and changes only when the Device reports it; one that no source feeds holds the value a Flow wrote.
    /// </summary>
    /// <param name="namespace">The Namespace of the Asset.</param>
    /// <param name="asset">The name of the Asset.</param>
    /// <param name="dataPoint">The name of the Data Point.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or the reason the write was refused or failed.</returns>
    Task<Result> WriteDataPointAsync(
        string @namespace,
        string asset,
        string dataPoint,
        JsonElement value,
        CancellationToken cancellationToken);
}
