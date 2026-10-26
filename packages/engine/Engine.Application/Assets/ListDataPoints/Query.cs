using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Engine.Application.Devices;

namespace Engine.Application.Assets.ListDataPoints;

/// <summary>
/// Lists an Asset's Data Points with their latest value, Quality and source timestamp.
/// </summary>
/// <param name="Namespace">The Namespace of the Asset.</param>
/// <param name="Asset">The name of the Asset.</param>
public sealed record Query(string Namespace, string Asset) : IRequest<Result<IReadOnlyList<DataPointReading>>>;

internal sealed class Handler(IAssetRuntime assets) : IRequestHandler<Query, Result<IReadOnlyList<DataPointReading>>>
{
    public Task<Result<IReadOnlyList<DataPointReading>>> HandleAsync(
        Query request,
        CancellationToken cancellationToken = default) =>
        assets.ListDataPointsAsync(request.Namespace, request.Asset, cancellationToken);
}
