using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Domain.Authorization;
using Engine.Application.Authorization;

namespace Engine.Application.Flows.CancelFlowExecutions;

/// <summary>
/// Cancels every running or waiting Flow Execution of a Flow and empties its queue. The caller must hold the
/// Engineer role.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Flow.</param>
/// <param name="Flow">The name of the Flow.</param>
public sealed record Command(Guid CallerId, string Namespace, string Flow) : IRequest<Result<int>>;

internal sealed class Handler(IRoleGrants grants, IFlowRuntime flows) : IRequestHandler<Command, Result<int>>
{
    public async Task<Result<int>> HandleAsync(Command request, CancellationToken cancellationToken = default)
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

        return await flows.CancelAllAsync(request.Namespace, request.Flow, cancellationToken);
    }
}
