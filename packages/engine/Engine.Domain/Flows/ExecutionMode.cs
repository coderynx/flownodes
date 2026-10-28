namespace Engine.Domain.Flows;

/// <summary>
/// What a Flow does when it is triggered while one of its Flow Executions is still running.
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// Drops the new trigger and records the drop.
    /// </summary>
    Single,

    /// <summary>
    /// Cancels the running Flow Execution and starts again.
    /// </summary>
    Restart,

    /// <summary>
    /// Keeps the trigger and runs it after the running Flow Executions.
    /// </summary>
    Queue,

    /// <summary>
    /// Runs the new Flow Execution beside the running ones.
    /// </summary>
    Parallel
}

/// <summary>
/// What a Flow does with one trigger, given its Execution Mode and what is already running.
/// </summary>
public enum FlowAdmission
{
    /// <summary>
    /// Starts a Flow Execution now.
    /// </summary>
    Start,

    /// <summary>
    /// Drops the trigger and records the drop.
    /// </summary>
    Drop,

    /// <summary>
    /// Keeps the trigger until a running Flow Execution finishes.
    /// </summary>
    Queue,

    /// <summary>
    /// Cancels the running Flow Executions, then starts one.
    /// </summary>
    Restart
}
