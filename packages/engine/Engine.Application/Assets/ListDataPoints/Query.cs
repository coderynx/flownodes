using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Engine.Application.Authorization;
using Engine.Application.Devices;

namespace Engine.Application.Assets.ListDataPoints;

/// <summary>
/// Lists an Asset's Data Points with their latest value, Quality and source timestamp.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Asset.</param>
/// <param name="Asset">The name of the Asset.</param>
public sealed record Query(Guid CallerId, string Namespace, string Asset)
    : IRequest<Result<IReadOnlyList<DataPointReading>>>;

internal sealed class Handler(IRoleGrants grants, IAssetRuntime assets)
    : IRequestHandler<Query, Result<IReadOnlyList<DataPointReading>>>
{
    public async Task<Result<IReadOnlyList<DataPointReading>>> HandleAsync(
        Query request,
        CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireMemberAsync(request.CallerId, request.Namespace, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return await assets.ListDataPointsAsync(request.Namespace, request.Asset, cancellationToken);
    }
}
