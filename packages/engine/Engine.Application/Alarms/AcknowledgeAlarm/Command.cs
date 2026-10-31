using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Coderynx.MediatorKit.Abstractions;
using Core.Application.Authorization;
using Core.Domain.Authorization;
using Engine.Application.Authorization;

namespace Engine.Application.Alarms.AcknowledgeAlarm;

/// <summary>
/// Acknowledges a live Alarm, active or cleared. The caller must hold the Operator role.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="CallerName">The <c>name</c> claim of the caller's access token, which the Alarm keeps.</param>
/// <param name="Namespace">The Namespace of the Alarm.</param>
/// <param name="Id">The id of the Alarm.</param>
/// <param name="Comment">An optional comment kept with the acknowledgement.</param>
public sealed record Command(Guid CallerId, string? CallerName, string Namespace, Guid Id, string? Comment)
    : IRequest<Result>;

internal sealed class Handler(IRoleGrants grants, IAlarmRuntime alarms) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireRoleAsync(
            request.CallerId,
            request.Namespace,
            NamespaceRole.Operator,
            cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        if (string.IsNullOrEmpty(request.CallerName))
        {
            return Error.Custom(AccessErrors.UnauthenticatedCode, "The access token carries no name claim.");
        }

        return await alarms.AcknowledgeAsync(
            request.Namespace,
            request.Id,
            request.CallerId,
            request.CallerName,
            request.Comment,
            cancellationToken);
    }
}
