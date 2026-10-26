using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Domain.Authorization;
using Engine.Application.Authorization;

namespace Engine.Application.Alarms.AcknowledgeAlarm;

/// <summary>
/// Acknowledges a live Alarm, active or cleared. The caller must hold the Operator or Engineer role.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Alarm.</param>
/// <param name="Id">The id of the Alarm.</param>
/// <param name="Comment">An optional comment kept with the acknowledgement.</param>
public sealed record Command(Guid CallerId, string Namespace, Guid Id, string? Comment) : IRequest<Result>;

internal sealed class Handler(IRoleGrants grants, IAlarmRuntime alarms) : IRequestHandler<Command, Result>
{
    private static readonly NamespaceRole[] Acknowledgers = [NamespaceRole.Operator, NamespaceRole.Engineer];

    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireAnyRoleAsync(
            request.CallerId,
            request.Namespace,
            Acknowledgers,
            cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return await alarms.AcknowledgeAsync(
            request.Namespace,
            request.Id,
            request.CallerId,
            request.Comment,
            cancellationToken);
    }
}
