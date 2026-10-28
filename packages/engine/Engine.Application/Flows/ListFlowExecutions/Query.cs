using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Coderynx.MediatorKit.Abstractions;
using Engine.Application.Authorization;

namespace Engine.Application.Flows.ListFlowExecutions;

/// <summary>
/// Lists the Flow Executions of a Flow, with where each one is.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Flow.</param>
/// <param name="Flow">The name of the Flow.</param>
/// <param name="Q">
/// Keeps only Flow Executions in this state, or whose trigger or current Node id contains it; none for all.
/// </param>
/// <param name="Sort">The key to sort by: <c>startedAt</c>, the default, or <c>finishedAt</c>.</param>
/// <param name="Desc">Reverses the sort order.</param>
/// <param name="Offset">How many Flow Executions to skip; negative values count as zero.</param>
/// <param name="Limit">How many Flow Executions to return; clamped to 1 through 200.</param>
public sealed record Query(
    Guid CallerId,
    string Namespace,
    string Flow,
    string? Q,
    string? Sort,
    bool Desc,
    int Offset,
    int Limit) : IRequest<Result<FlowExecutionPage>>;

internal sealed class Handler(IRoleGrants grants, IFlowRuntime flows)
    : IRequestHandler<Query, Result<FlowExecutionPage>>
{
    private const int MaxLimit = 200;

    private static readonly Dictionary<string, FlowExecutionOrder> Keys = new()
    {
        ["startedAt"] = FlowExecutionOrder.StartedAt,
        ["finishedAt"] = FlowExecutionOrder.FinishedAt
    };

    public async Task<Result<FlowExecutionPage>> HandleAsync(
        Query request,
        CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireMemberAsync(request.CallerId, request.Namespace, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        if (!Keys.TryGetValue(request.Sort ?? "startedAt", out var order))
        {
            return Error.InvalidInput(
                "flow_execution.unknown_sort",
                $"Sort by one of {string.Join(", ", Keys.Keys)}, not '{request.Sort}'.");
        }

        return Result.Found(
            await flows.ListAsync(
                request.Namespace,
                request.Flow,
                string.IsNullOrWhiteSpace(request.Q) ? null : request.Q,
                order,
                request.Desc,
                Math.Max(request.Offset, 0),
                Math.Clamp(request.Limit, 1, MaxLimit),
                cancellationToken));
    }
}
