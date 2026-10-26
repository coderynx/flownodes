using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Engine.Application.Authorization;
using Engine.Domain.Alarms;

namespace Engine.Application.Alarms.ListAlarms;

/// <summary>
/// Lists the live Alarms of a Namespace, critical first and then by when they were raised.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace.</param>
public sealed record Query(Guid CallerId, string Namespace) : IRequest<Result<IReadOnlyList<Alarm>>>;

internal sealed class Handler(IRoleGrants grants, IAlarmRuntime alarms)
    : IRequestHandler<Query, Result<IReadOnlyList<Alarm>>>
{
    public async Task<Result<IReadOnlyList<Alarm>>> HandleAsync(
        Query request,
        CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireMemberAsync(request.CallerId, request.Namespace, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return Result.Found(await alarms.ListLiveAsync(request.Namespace, cancellationToken));
    }
}
