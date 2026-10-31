using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Engine.Application.Authorization;
using Engine.Domain.Alarms;

namespace Engine.Application.Alarms.GetAlarm;

/// <summary>
/// Reads one Alarm of a Namespace, live or ended, until its retention runs out.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Alarm.</param>
/// <param name="Id">The id of the Alarm.</param>
public sealed record Query(Guid CallerId, string Namespace, Guid Id) : IRequest<Result<Alarm>>;

internal sealed class Handler(IRoleGrants grants, IAlarmRuntime alarms) : IRequestHandler<Query, Result<Alarm>>
{
    public async Task<Result<Alarm>> HandleAsync(Query request, CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireMemberAsync(request.CallerId, request.Namespace, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return await alarms.GetAsync(request.Namespace, request.Id, cancellationToken);
    }
}
