using System.Text.Json;
using Coderynx.Functional.Results;
using Engine.Domain.Flows;

namespace Engine.Application.Flows;

/// <summary>
/// The live side of Flows: their Flow Executions, which the Engine keeps as runtime state (ADR-0015).
/// </summary>
public interface IFlowRuntime
{
    /// <summary>
    /// Lists the Flow Executions of a Flow in a given order.
    /// </summary>
    /// <param name="namespace">The Namespace of the Flow.</param>
    /// <param name="flow">The name of the Flow.</param>
    /// <param name="filter">
    /// Keeps only Flow Executions in this state, or whose trigger or current Node id contains it; none for all.
    /// </param>
    /// <param name="order">What to order them by.</param>
    /// <param name="descending">Whether to reverse the order.</param>
    /// <param name="offset">How many Flow Executions to skip.</param>
    /// <param name="limit">How many Flow Executions to return.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>One page of Flow Executions.</returns>
    Task<FlowExecutionPage> ListAsync(
        string @namespace,
        string flow,
        string? filter,
        FlowExecutionOrder order,
        bool descending,
        int offset,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads one Flow Execution.
    /// </summary>
    /// <param name="namespace">The Namespace of the Flow.</param>
    /// <param name="flow">The name of the Flow.</param>
    /// <param name="id">The id of the Flow Execution.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The Flow Execution, or a not-found error when the Flow has none with that id.</returns>
    Task<Result<FlowExecution>> GetAsync(string @namespace, string flow, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a running or waiting Flow Execution at its next checkpoint; a Node in progress is not interrupted.
    /// </summary>
    /// <param name="namespace">The Namespace of the Flow.</param>
    /// <param name="flow">The name of the Flow.</param>
    /// <param name="id">The id of the Flow Execution.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>
    /// Success, a not-found error when the Flow has no such Flow Execution, or a conflict when it has finished.
    /// </returns>
    Task<Result> CancelAsync(string @namespace, string flow, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels every running or waiting Flow Execution of a Flow and empties its queue.
    /// </summary>
    /// <param name="namespace">The Namespace of the Flow.</param>
    /// <param name="flow">The name of the Flow.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>
    /// How many Flow Executions were cancelled, or a not-found error when the Engine knows no such Flow.
    /// </returns>
    Task<Result<int>> CancelAllAsync(string @namespace, string flow, CancellationToken cancellationToken);

    /// <summary>
    /// Starts a Flow Execution by hand, from one of the Flow's triggers, as its Execution Mode allows.
    /// </summary>
    /// <param name="namespace">The Namespace of the Flow.</param>
    /// <param name="flow">The name of the Flow.</param>
    /// <param name="trigger">The trigger Node to start from; none when the Flow has exactly one.</param>
    /// <param name="payload">What the trigger carries, kept in the <c>trigger</c> variable.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>
    /// What became of the trigger, a not-found error when the Engine knows no such Flow, an invalid-input error for an
    /// unknown or ambiguous trigger, or a conflict when the Execution Mode dropped it.
    /// </returns>
    Task<Result<ExecutionStart>> StartAsync(
        string @namespace,
        string flow,
        string? trigger,
        JsonElement payload,
        CancellationToken cancellationToken);
}

/// <summary>
/// One page of Flow Executions.
/// </summary>
/// <param name="Items">The Flow Executions on this page.</param>
/// <param name="Total">How many Flow Executions the Flow has, across all pages.</param>
public sealed record FlowExecutionPage(IReadOnlyList<FlowExecution> Items, int Total);

/// <summary>
/// What to order a list of Flow Executions by.
/// </summary>
public enum FlowExecutionOrder
{
    /// <summary>
    /// When each started.
    /// </summary>
    StartedAt,

    /// <summary>
    /// When each finished; those still running or waiting come last.
    /// </summary>
    FinishedAt
}

/// <summary>
/// What became of a trigger given by hand.
/// </summary>
/// <param name="Execution">The Flow Execution it started; none when it was queued.</param>
/// <param name="Queued">Whether it waits in the Flow's queue.</param>
public sealed record ExecutionStart(Guid? Execution, bool Queued);
