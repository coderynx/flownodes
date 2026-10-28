using System.Text.Json;
using Core.Domain.Scheduling;

namespace Engine.Domain.Flows;

/// <summary>
/// The graph of Nodes a Flow's Spec describes, as one Generation of it runs.
/// </summary>
public sealed class FlowDefinition
{
    /// <summary>
    /// How many Flow Executions may run at once, and how many triggers may wait, in <c>queue</c> and
    /// <c>parallel</c> mode.
    /// </summary>
    public const int MaxConcurrent = 10;

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly Dictionary<string, FlowNode> _nodes;

    private FlowDefinition(ExecutionMode mode, Dictionary<string, FlowNode> nodes)
    {
        Mode = mode;
        _nodes = nodes;
    }

    /// <summary>
    /// Gets the Flow's Execution Mode.
    /// </summary>
    public ExecutionMode Mode { get; }

    /// <summary>
    /// Gets the Flow's triggers, in the order the Spec lists them.
    /// </summary>
    public IReadOnlyList<TriggerNode> Triggers => [.. _nodes.Values.OfType<TriggerNode>()];

    /// <summary>
    /// Builds the definition from a Spec the Registry has already validated.
    /// </summary>
    /// <param name="spec">The Flow's Spec.</param>
    /// <returns>The definition.</returns>
    public static FlowDefinition Create(JsonElement spec)
    {
        var mode = spec.TryGetProperty("executionMode", out var given)
            ? Enum.Parse<ExecutionMode>(given.GetString()!, true)
            : ExecutionMode.Single;
        var nodes = spec.GetProperty("nodes").EnumerateArray().Select(Node).ToDictionary(node => node.Id);

        return new FlowDefinition(mode, nodes);
    }

    /// <summary>
    /// Decides what one trigger does, given the Execution Mode and what is already running.
    /// </summary>
    /// <param name="mode">The Flow's Execution Mode.</param>
    /// <param name="running">How many of its Flow Executions are running or waiting.</param>
    /// <param name="queued">How many triggers already wait in its queue.</param>
    /// <returns>What to do with the trigger.</returns>
    public static FlowAdmission Admit(ExecutionMode mode, int running, int queued) =>
        mode switch
        {
            _ when running == 0 => FlowAdmission.Start,
            ExecutionMode.Restart => FlowAdmission.Restart,
            ExecutionMode.Queue when queued < MaxConcurrent => FlowAdmission.Queue,
            ExecutionMode.Parallel when running < MaxConcurrent => FlowAdmission.Start,
            _ => FlowAdmission.Drop
        };

    /// <summary>
    /// Finds a Node by its id.
    /// </summary>
    /// <param name="id">The id of the Node.</param>
    /// <returns>The Node, or <see langword="null"/> when the Flow has none with that id.</returns>
    public FlowNode? Node(string id) => _nodes.GetValueOrDefault(id);

    private static FlowNode Node(JsonElement node)
    {
        var id = node.GetProperty("id").GetString()!;
        var next = Text(node, "next");

        return node.GetProperty("type").GetString() switch
        {
            "trigger" => new TriggerNode(
                id,
                next,
                DataPoint(node, "dataPoint"),
                Comparison(node, "when"),
                Member(node, "event"),
                Schedule(node)),
            "read" => new ReadNode(id, next, Policy(node), DataPoint(node, "dataPoint")!, Text(node, "into")),
            "write" => new WriteNode(
                id,
                next,
                Policy(node),
                DataPoint(node, "dataPoint")!,
                node.TryGetProperty("value", out var value) ? value.GetRawText() : null,
                Text(node, "from")),
            "invoke" => new InvokeNode(
                id,
                next,
                Policy(node),
                Member(node, "action")!,
                node.TryGetProperty("parameters", out var parameters) ? parameters.GetRawText() : "{}",
                Text(node, "from"),
                Text(node, "into")),
            "branch" => new BranchNode(
                id,
                node.GetProperty("if").GetProperty("from").GetString()!,
                Comparison(node, "if")!,
                Text(node, "then"),
                Text(node, "else")),
            _ => new WaitNode(
                id,
                next,
                Text(node, "error"),
                Text(node, "timedOut"),
                Duration(node, "for"),
                node.TryGetProperty("until", out var until)
                    ? new WaitCondition(DataPoint(until, "dataPoint")!, Comparison(node, "until")!)
                    : null,
                Member(node, "event"),
                Duration(node, "timeout"),
                Duration(node, "maxLateness"),
                Text(node, "into"))
        };
    }

    private static CallPolicy Policy(JsonElement node)
    {
        var retry = node.TryGetProperty("retry", out var given) ? given : default;

        return new CallPolicy(
            Text(node, "error"),
            Duration(node, "timeout") ?? DefaultTimeout,
            retry.ValueKind == JsonValueKind.Object ? retry.GetProperty("attempts").GetInt32() : 0,
            retry.ValueKind == JsonValueKind.Object ? Duration(retry, "delay") ?? TimeSpan.Zero : TimeSpan.Zero);
    }

    private static DataPointRef? DataPoint(JsonElement node, string field)
    {
        if (!node.TryGetProperty(field, out var target))
        {
            return null;
        }

        var name = target.GetProperty("name").GetString()!;

        return Text(target, "asset") is { } asset
            ? new DataPointRef(DataPointOwner.Asset, asset, name)
            : new DataPointRef(DataPointOwner.Device, Text(target, "device")!, name);
    }

    private static MemberRef? Member(JsonElement node, string field) =>
        node.TryGetProperty(field, out var target)
            ? new MemberRef(target.GetProperty("device").GetString()!, target.GetProperty("name").GetString()!)
            : null;

    private static Comparison? Comparison(JsonElement node, string field) =>
        node.TryGetProperty(field, out var comparison)
            ? new Comparison(
                comparison.GetProperty("operator").GetString()!,
                comparison.GetProperty("value").GetRawText())
            : null;

    private static ScheduleTrigger? Schedule(JsonElement node) =>
        node.TryGetProperty("schedule", out var schedule)
            ? new ScheduleTrigger(
                CronSchedule.Create(schedule.GetProperty("cron").GetString()!),
                Text(schedule, "timeZone"),
                Duration(schedule, "maxLateness"))
            : null;

    private static TimeSpan? Duration(JsonElement node, string field) =>
        Text(node, field) is { } text ? Durations.Parse(text) : null;

    private static string? Text(JsonElement node, string name) =>
        node.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
