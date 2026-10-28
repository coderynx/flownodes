using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Engine.Application.Authorization;
using Engine.Domain.Flows;

namespace Engine.Application.Flows.GetFlowExecution;

/// <summary>
/// Reads one Flow Execution of a Flow, with where it is and its variables.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Flow.</param>
/// <param name="Flow">The name of the Flow.</param>
/// <param name="Id">The id of the Flow Execution.</param>
public sealed record Query(Guid CallerId, string Namespace, string Flow, Guid Id) : IRequest<Result<FlowExecution>>;

internal sealed class Handler(IRoleGrants grants, IFlowRuntime flows) : IRequestHandler<Query, Result<FlowExecution>>
{
    public async Task<Result<FlowExecution>> HandleAsync(Query request, CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireMemberAsync(request.CallerId, request.Namespace, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return await flows.GetAsync(request.Namespace, request.Flow, request.Id, cancellationToken);
    }
}
