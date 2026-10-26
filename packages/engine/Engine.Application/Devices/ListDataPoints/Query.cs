using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Engine.Application.Authorization;

namespace Engine.Application.Devices.ListDataPoints;

/// <summary>
/// Lists a Device's Data Points with their latest value, Quality and source timestamp.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Device.</param>
/// <param name="Device">The name of the Device.</param>
public sealed record Query(Guid CallerId, string Namespace, string Device)
    : IRequest<Result<IReadOnlyList<DataPointReading>>>;

internal sealed class Handler(IRoleGrants grants, IDeviceRuntime devices)
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

        return await devices.ListDataPointsAsync(request.Namespace, request.Device, cancellationToken);
    }
}
