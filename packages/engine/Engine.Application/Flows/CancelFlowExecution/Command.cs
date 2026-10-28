using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Domain.Authorization;
using Engine.Application.Authorization;

namespace Engine.Application.Flows.CancelFlowExecution;

/// <summary>
/// Cancels a running or waiting Flow Execution at its next checkpoint. The caller must hold the Engineer role.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Flow.</param>
/// <param name="Flow">The name of the Flow.</param>
/// <param name="Id">The id of the Flow Execution.</param>
public sealed record Command(Guid CallerId, string Namespace, string Flow, Guid Id) : IRequest<Result>;

internal sealed class Handler(IRoleGrants grants, IFlowRuntime flows) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireRoleAsync(
            request.CallerId,
            request.Namespace,
            NamespaceRole.Engineer,
            cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return await flows.CancelAsync(request.Namespace, request.Flow, request.Id, cancellationToken);
    }
}
