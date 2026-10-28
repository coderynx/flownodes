using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Engine.Application.Authorization;

namespace Engine.Application.Flows.ListFlowExecutions;

/// <summary>
/// Lists the Flow Executions of a Flow, newest first, with where each one is.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Flow.</param>
/// <param name="Flow">The name of the Flow.</param>
/// <param name="Offset">How many Flow Executions to skip; negative values count as zero.</param>
/// <param name="Limit">How many Flow Executions to return; clamped to 1 through 200.</param>
public sealed record Query(Guid CallerId, string Namespace, string Flow, int Offset, int Limit)
    : IRequest<Result<FlowExecutionPage>>;

internal sealed class Handler(IRoleGrants grants, IFlowRuntime flows)
    : IRequestHandler<Query, Result<FlowExecutionPage>>
{
    private const int MaxLimit = 200;

    public async Task<Result<FlowExecutionPage>> HandleAsync(
        Query request,
        CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireMemberAsync(request.CallerId, request.Namespace, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return Result.Found(
            await flows.ListAsync(
                request.Namespace,
                request.Flow,
                Math.Max(request.Offset, 0),
                Math.Clamp(request.Limit, 1, MaxLimit),
                cancellationToken));
    }
}
