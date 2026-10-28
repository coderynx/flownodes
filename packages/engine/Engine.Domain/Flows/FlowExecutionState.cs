namespace Engine.Domain.Flows;

/// <summary>
/// Where a Flow Execution is.
/// </summary>
public enum FlowExecutionState
{
    /// <summary>
    /// Running its current Node.
    /// </summary>
    Running,

    /// <summary>
    /// Waiting at a wait Node until its deadline, condition or Event.
    /// </summary>
    Waiting,

    /// <summary>
    /// Finished by reaching a Node with nowhere left to go.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Finished by a Node that failed with no error edge.
    /// </summary>
    Failed,

    /// <summary>
    /// Stopped because an Engineer cancelled it, its Flow was deleted or a <c>restart</c> trigger replaced it.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Never run, because its Execution Mode refused the trigger or a schedule fire was too late.
    /// </summary>
    Dropped
}

/// <summary>
/// Why a Flow Execution failed or was dropped.
/// </summary>
/// <param name="Code">What went wrong, such as <c>Timeout</c>, <c>ReferenceMissing</c> or <c>Busy</c>.</param>
/// <param name="Message">What went wrong, for Engineers.</param>
/// <param name="Node">The Node where it went wrong.</param>
/// <param name="Attempts">How many attempts that Node made.</param>
public sealed record FlowExecutionError(string Code, string Message, string Node, int Attempts);

/// <summary>
/// Everything a persisted Flow Execution checkpoint holds.
/// </summary>
/// <param name="Id">The id of the Flow Execution.</param>
/// <param name="Namespace">The Namespace of its Flow.</param>
/// <param name="Flow">The name of its Flow.</param>
/// <param name="FlowUid">The <c>uid</c> of its Flow.</param>
/// <param name="Generation">The Generation it runs.</param>
/// <param name="Spec">The Spec of that Generation.</param>
/// <param name="Trigger">The id of the trigger Node that started it.</param>
/// <param name="State">Where it is.</param>
/// <param name="CurrentNode">The Node it is at.</param>
/// <param name="Variables">Its variables, as a JSON object.</param>
/// <param name="Visits">How many times it completed each Node, as a JSON object.</param>
/// <param name="Steps">How many Nodes it completed.</param>
/// <param name="Deadline">When its wait ends.</param>
/// <param name="Lateness">How late its last wait continued.</param>
/// <param name="Error">Why it failed or was dropped.</param>
/// <param name="CancelRequested">Whether it was asked to stop.</param>
/// <param name="StartedAt">When it started.</param>
/// <param name="UpdatedAt">When it last moved.</param>
/// <param name="FinishedAt">When it finished.</param>
public sealed record FlowExecutionCheckpoint(
    Guid Id,
    string Namespace,
    string Flow,
    Guid FlowUid,
    long Generation,
    string Spec,
    string Trigger,
    FlowExecutionState State,
    string? CurrentNode,
    string Variables,
    string Visits,
    int Steps,
    DateTimeOffset? Deadline,
    TimeSpan? Lateness,
    FlowExecutionError? Error,
    bool CancelRequested,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? FinishedAt);
