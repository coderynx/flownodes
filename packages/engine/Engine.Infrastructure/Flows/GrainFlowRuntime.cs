using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Engine.Application.Flows;
using Engine.Domain.Flows;

namespace Engine.Infrastructure.Flows;

internal sealed class GrainFlowRuntime(IGrainFactory grains, IFlowExecutions executions) : IFlowRuntime
{
    public Task<FlowExecutionPage> ListAsync(
        string @namespace,
        string flow,
        int offset,
        int limit,
        CancellationToken cancellationToken) =>
        executions.PageAsync(@namespace, flow, offset, limit, cancellationToken);

    public async Task<Result<FlowExecution>> GetAsync(
        string @namespace,
        string flow,
        Guid id,
        CancellationToken cancellationToken)
    {
        var execution = await executions.FindAsync(id, cancellationToken);
        var belongs = execution?.Namespace == @namespace && execution.Flow == flow;
        if (execution is null || !belongs)
        {
            return NotFound(id);
        }

        return Result.Found(execution);
    }

    public async Task<Result> CancelAsync(string @namespace, string flow, Guid id, CancellationToken cancellationToken)
    {
        var found = await GetAsync(@namespace, flow, id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        var cancelled = !found.Value.IsFinished &&
                        await grains.GetGrain<IFlowExecutionGrain>($"{@namespace}/{flow}/{id}").CancelAsync();

        return cancelled
            ? Result.Updated()
            : Error.Conflict("flow_execution.finished", $"Flow Execution '{id}' has already finished.");
    }

    public async Task<Result<int>> CancelAllAsync(string @namespace, string flow, CancellationToken cancellationToken)
    {
        var cancelled = await Flow(@namespace, flow).CancelAllAsync();

        return cancelled is { } count ? Result.Found(count) : FlowMissing(flow);
    }

    public async Task<Result<FlowRun>> RunAsync(
        string @namespace,
        string flow,
        string? trigger,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        var result = await Flow(@namespace, flow).RunAsync(trigger, payload.GetRawText());

        return result.Outcome switch
        {
            RunOutcome.Started => Result.Found(new FlowRun(result.Execution, false)),
            RunOutcome.Queued => Result.Found(new FlowRun(null, true)),
            RunOutcome.Dropped => Error.Conflict(
                "flow.busy",
                $"Flow '{flow}' is running and its Execution Mode dropped the trigger."),
            RunOutcome.TriggerUnknown => Error.InvalidInput(
                "flow.trigger_unknown",
                trigger is null
                    ? $"Flow '{flow}' has more than one trigger; name the one to start from."
                    : $"Flow '{flow}' has no trigger '{trigger}'."),
            _ => FlowMissing(flow)
        };
    }

    private IFlowGrain Flow(string @namespace, string flow) => grains.GetGrain<IFlowGrain>($"{@namespace}/{flow}");

    private static Error NotFound(Guid id) =>
        Error.NotFound("flow_execution.not_found", $"Flow Execution '{id}' does not exist.");

    private static Error FlowMissing(string flow) => Error.NotFound("flow.not_found", $"Flow '{flow}' does not exist.");
}
