using Core.Domain.Scheduling;

namespace Engine.Domain.Flows;

/// <summary>
/// A single step in a Flow.
/// </summary>
/// <param name="Id">The id of the Node, unique within its Flow.</param>
/// <param name="Next">The Node that follows on success; none to end the Flow Execution.</param>
public abstract record FlowNode(string Id, string? Next)
{
    /// <summary>
    /// Gets the Node that follows when this one fails; none to end the Flow Execution <c>Failed</c>.
    /// </summary>
    public virtual string? Error => null;
    /// <summary>
    /// Gets the variable the Node keeps its value in; none to keep nothing.
    /// </summary>
    public virtual string? Into => null;
}

/// <summary>
/// A Node that starts a Flow Execution: on a Data Point change or condition, an Event, a schedule, or only by hand
/// when it has none of these.
/// </summary>
/// <param name="Id">The id of the Node.</param>
/// <param name="Next">The Node the Flow Execution starts at.</param>
/// <param name="DataPoint">The Data Point whose changes trigger, if any.</param>
/// <param name="When">
/// The condition on that Data Point whose turning true triggers; none to trigger on any change.
/// </param>
/// <param name="Event">The Event that triggers, if any.</param>
/// <param name="Schedule">The schedule that triggers, if any.</param>
public sealed record TriggerNode(
    string Id,
    string? Next,
    DataPointRef? DataPoint,
    Comparison? When,
    MemberRef? Event,
    ScheduleTrigger? Schedule) : FlowNode(Id, Next);

/// <summary>
/// A Node that reads a Data Point and keeps its value, Quality and timestamp.
/// </summary>
/// <param name="Id">The id of the Node.</param>
/// <param name="Next">The Node that follows.</param>
/// <param name="Policy">The timeout, retries and error edge of the read.</param>
/// <param name="DataPoint">The Data Point to read.</param>
/// <param name="Keep">The variable that keeps the reading, if any.</param>
public sealed record ReadNode(string Id, string? Next, CallPolicy Policy, DataPointRef DataPoint, string? Keep)
    : FlowNode(Id, Next)
{
    /// <inheritdoc />
    public override string? Error => Policy.Error;
    /// <inheritdoc />
    public override string? Into => Keep;
}

/// <summary>
/// A Node that writes a Data Point, either a constant or the value of a variable.
/// </summary>
/// <param name="Id">The id of the Node.</param>
/// <param name="Next">The Node that follows.</param>
/// <param name="Policy">The timeout, retries and error edge of the write.</param>
/// <param name="DataPoint">The Data Point to write.</param>
/// <param name="Value">The constant to write, as JSON text, when there is no variable.</param>
/// <param name="From">The variable whose value is written, when there is no constant.</param>
public sealed record WriteNode(
    string Id,
    string? Next,
    CallPolicy Policy,
    DataPointRef DataPoint,
    string? Value,
    string? From) : FlowNode(Id, Next)
{
    /// <inheritdoc />
    public override string? Error => Policy.Error;
}

/// <summary>
/// A Node that invokes an Action of a Device and may keep its result.
/// </summary>
/// <param name="Id">The id of the Node.</param>
/// <param name="Next">The Node that follows.</param>
/// <param name="Policy">The timeout, retries and error edge of the call.</param>
/// <param name="Action">The Action to invoke.</param>
/// <param name="Parameters">The constant parameters, as JSON text, when there is no variable.</param>
/// <param name="From">The variable holding the parameters, when there are no constant ones.</param>
/// <param name="Keep">The variable that keeps the result, if any.</param>
public sealed record InvokeNode(
    string Id,
    string? Next,
    CallPolicy Policy,
    MemberRef Action,
    string Parameters,
    string? From,
    string? Keep) : FlowNode(Id, Next)
{
    /// <inheritdoc />
    public override string? Error => Policy.Error;
    /// <inheritdoc />
    public override string? Into => Keep;
}

/// <summary>
/// A Node that takes one of two edges by comparing a variable. A missing variable takes the <c>else</c> edge.
/// </summary>
/// <param name="Id">The id of the Node.</param>
/// <param name="From">The variable to compare.</param>
/// <param name="If">The comparison.</param>
/// <param name="Then">The Node that follows when the comparison holds; none to end.</param>
/// <param name="Else">The Node that follows otherwise; none to end.</param>
public sealed record BranchNode(string Id, string From, Comparison If, string? Then, string? Else) : FlowNode(Id, null);

/// <summary>
/// A Node that waits for a duration, for a Data Point condition or for an Event, the last two within a timeout.
/// </summary>
/// <param name="Id">The id of the Node.</param>
/// <param name="Next">The Node that follows once the wait is over.</param>
/// <param name="ErrorEdge">
/// The Node that follows when the wait is too late or times out without a <c>timedOut</c> edge.
/// </param>
/// <param name="TimedOut">The Node that follows when the wait times out or is too late.</param>
/// <param name="For">How long to wait, for a wait on a duration.</param>
/// <param name="Until">The Data Point condition to wait for.</param>
/// <param name="Event">The Event to wait for.</param>
/// <param name="Timeout">How long to wait for the condition or Event.</param>
/// <param name="MaxLateness">How late past its deadline the wait may still continue normally.</param>
/// <param name="Keep">The variable that keeps the Event's payload, if any.</param>
public sealed record WaitNode(
    string Id,
    string? Next,
    string? ErrorEdge,
    string? TimedOut,
    TimeSpan? For,
    WaitCondition? Until,
    MemberRef? Event,
    TimeSpan? Timeout,
    TimeSpan? MaxLateness,
    string? Keep) : FlowNode(Id, Next)
{
    /// <inheritdoc />
    public override string? Error => ErrorEdge;
    /// <inheritdoc />
    public override string? Into => Keep;
}

/// <summary>
/// Whether a Data Point belongs to an Asset or a Device.
/// </summary>
public enum DataPointOwner
{
    /// <summary>
    /// An Asset Data Point.
    /// </summary>
    Asset,

    /// <summary>
    /// A Device Data Point.
    /// </summary>
    Device
}

/// <summary>
/// A Data Point of an Asset or a Device, by name.
/// </summary>
/// <param name="Owner">Whether it belongs to an Asset or a Device.</param>
/// <param name="Resource">The name of the Asset or Device.</param>
/// <param name="Name">The name of the Data Point.</param>
public sealed record DataPointRef(DataPointOwner Owner, string Resource, string Name);

/// <summary>
/// An Action or Event of a Device, by name.
/// </summary>
/// <param name="Device">The name of the Device.</param>
/// <param name="Name">The name of the Action or Event.</param>
public sealed record MemberRef(string Device, string Name);

/// <summary>
/// A condition on a Data Point that a wait holds out for.
/// </summary>
/// <param name="DataPoint">The Data Point.</param>
/// <param name="Comparison">The comparison its value must satisfy.</param>
public sealed record WaitCondition(DataPointRef DataPoint, Comparison Comparison);

/// <summary>
/// A cron schedule that triggers a Flow.
/// </summary>
/// <param name="Cron">The schedule.</param>
/// <param name="TimeZone">The IANA time zone it is read in; none for the installation-wide one.</param>
/// <param name="MaxLateness">How late a fire may still run; a later one is skipped. None to always run.</param>
public sealed record ScheduleTrigger(CronSchedule Cron, string? TimeZone, TimeSpan? MaxLateness);

/// <summary>
/// How a Node that calls out to an Asset or Device behaves when it is slow or fails.
/// </summary>
/// <param name="Error">The Node that follows a failure; none to end the Flow Execution <c>Failed</c>.</param>
/// <param name="Timeout">How long each attempt may take.</param>
/// <param name="Retries">How many times a failed attempt is repeated.</param>
/// <param name="RetryDelay">How long to wait before repeating.</param>
public sealed record CallPolicy(string? Error, TimeSpan Timeout, int Retries, TimeSpan RetryDelay);
