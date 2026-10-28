using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Engine.Domain.Flows;

/// <summary>
/// One run of a Flow, pinned to the Generation and Spec it started on, at exactly one Node at a time. Every method
/// that moves it is a checkpoint the Engine persists (ADR-0006).
/// </summary>
public sealed class FlowExecution
{
    private Dictionary<string, int> _visits = [];
    private JsonObject _variables = [];
    private FlowGraph? _graph;

    private FlowExecution()
    {
    }

    /// <summary>
    /// Gets the id of the Flow Execution.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the Namespace of its Flow.
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// Gets the name of its Flow.
    /// </summary>
    public required string Flow { get; init; }

    /// <summary>
    /// Gets the <c>uid</c> of the Flow it belongs to; a re-created Flow has another.
    /// </summary>
    public required Guid FlowUid { get; init; }

    /// <summary>
    /// Gets the Generation of the Flow it runs.
    /// </summary>
    public required long Generation { get; init; }

    /// <summary>
    /// Gets the Spec of that Generation, as JSON text.
    /// </summary>
    public required string Spec { get; init; }

    /// <summary>
    /// Gets the id of the trigger Node that started it.
    /// </summary>
    public required string Trigger { get; init; }

    /// <summary>
    /// Gets when it started.
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Gets where it is.
    /// </summary>
    public FlowExecutionState State { get; private set; }

    /// <summary>
    /// Gets the Node it is at; none once it has finished.
    /// </summary>
    public string? CurrentNode { get; private set; }

    /// <summary>
    /// Gets how many Nodes it has completed, its trigger included.
    /// </summary>
    public int Steps { get; private set; }

    /// <summary>
    /// Gets when the wait it is in ends; none when it is not waiting.
    /// </summary>
    public DateTimeOffset? Deadline { get; private set; }

    /// <summary>
    /// Gets how late past its deadline its last wait continued; none when no wait was late.
    /// </summary>
    public TimeSpan? Lateness { get; private set; }

    /// <summary>
    /// Gets why it failed or was dropped; none otherwise.
    /// </summary>
    public FlowExecutionError? Error { get; private set; }

    /// <summary>
    /// Gets whether it has been asked to stop at its next checkpoint.
    /// </summary>
    public bool CancelRequested { get; private set; }

    /// <summary>
    /// Gets when it last moved.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Gets when it finished; none while it runs or waits.
    /// </summary>
    public DateTimeOffset? FinishedAt { get; private set; }

    /// <summary>
    /// Gets how many times it has completed each Node.
    /// </summary>
    public IReadOnlyDictionary<string, int> Visits => _visits;

    /// <summary>
    /// Gets its variables as a JSON object.
    /// </summary>
    public string Variables => _variables.ToJsonString();

    /// <summary>
    /// Gets whether it has finished, whatever the outcome.
    /// </summary>
    public bool IsFinished => FinishedAt is not null;

    /// <summary>
    /// Gets the graph of Nodes it runs.
    /// </summary>
    public FlowGraph Graph => _graph ??= FlowGraph.Create(JsonDocument.Parse(Spec).RootElement);

    /// <summary>
    /// Gets the Node it is at; none once it has finished.
    /// </summary>
    public FlowNode? Node => CurrentNode is { } id ? Graph.Node(id) : null;

    /// <summary>
    /// Gets the id an Action call from the current Node carries, which stays the same until the Node completes, so a
    /// call repeated after a failover can be recognised (ADR-0003).
    /// </summary>
    public string InvocationId => $"{Id}/{CurrentNode}/{_visits.GetValueOrDefault(CurrentNode ?? "") + 1}";

    /// <summary>
    /// Starts a Flow Execution at the Node its trigger leads to, keeping the trigger's payload in <c>trigger</c>.
    /// </summary>
    /// <param name="id">The id of the Flow Execution.</param>
    /// <param name="namespace">The Namespace of the Flow.</param>
    /// <param name="flow">The name of the Flow.</param>
    /// <param name="flowUid">The <c>uid</c> of the Flow.</param>
    /// <param name="generation">The Generation of the Flow it runs.</param>
    /// <param name="spec">The Spec of that Generation.</param>
    /// <param name="trigger">The id of the trigger Node.</param>
    /// <param name="payload">What the trigger carried.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The Flow Execution, finished already when the trigger leads nowhere.</returns>
    public static FlowExecution Start(
        Guid id,
        string @namespace,
        string flow,
        Guid flowUid,
        long generation,
        string spec,
        string trigger,
        JsonNode? payload,
        DateTimeOffset now)
    {
        var execution = new FlowExecution
        {
            Id = id,
            Namespace = @namespace,
            Flow = flow,
            FlowUid = flowUid,
            Generation = generation,
            Spec = spec,
            Trigger = trigger,
            StartedAt = now,
            CurrentNode = trigger,
            _variables = new JsonObject { ["trigger"] = payload?.DeepClone() }
        };
        execution.Advance(execution.Graph.Node(trigger)!.Next, now);

        return execution;
    }

    /// <summary>
    /// Records a trigger the Flow did not run, already finished as <c>Dropped</c>.
    /// </summary>
    /// <param name="id">The id of the record.</param>
    /// <param name="namespace">The Namespace of the Flow.</param>
    /// <param name="flow">The name of the Flow.</param>
    /// <param name="flowUid">The <c>uid</c> of the Flow.</param>
    /// <param name="generation">The Generation of the Flow.</param>
    /// <param name="spec">The Spec of that Generation.</param>
    /// <param name="trigger">The id of the trigger Node.</param>
    /// <param name="reason">Why it was dropped, such as <c>Busy</c> or <c>TooLate</c>.</param>
    /// <param name="message">The reason, for Engineers.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The record.</returns>
    public static FlowExecution Drop(
        Guid id,
        string @namespace,
        string flow,
        Guid flowUid,
        long generation,
        string spec,
        string trigger,
        string reason,
        string message,
        DateTimeOffset now) =>
        new()
        {
            Id = id,
            Namespace = @namespace,
            Flow = flow,
            FlowUid = flowUid,
            Generation = generation,
            Spec = spec,
            Trigger = trigger,
            StartedAt = now,
            State = FlowExecutionState.Dropped,
            Error = new FlowExecutionError(reason, message, trigger, 0),
            UpdatedAt = now,
            FinishedAt = now
        };

    /// <summary>
    /// Rebuilds a Flow Execution from its persisted checkpoint.
    /// </summary>
    /// <param name="checkpoint">Everything the checkpoint holds.</param>
    /// <returns>The Flow Execution.</returns>
    public static FlowExecution Restore(FlowExecutionCheckpoint checkpoint) =>
        new()
        {
            Id = checkpoint.Id,
            Namespace = checkpoint.Namespace,
            Flow = checkpoint.Flow,
            FlowUid = checkpoint.FlowUid,
            Generation = checkpoint.Generation,
            Spec = checkpoint.Spec,
            Trigger = checkpoint.Trigger,
            StartedAt = checkpoint.StartedAt,
            State = checkpoint.State,
            CurrentNode = checkpoint.CurrentNode,
            Steps = checkpoint.Steps,
            Deadline = checkpoint.Deadline,
            Lateness = checkpoint.Lateness,
            Error = checkpoint.Error,
            CancelRequested = checkpoint.CancelRequested,
            UpdatedAt = checkpoint.UpdatedAt,
            FinishedAt = checkpoint.FinishedAt,
            _visits = JsonSerializer.Deserialize<Dictionary<string, int>>(checkpoint.Visits) ?? [],
            _variables = JsonNode.Parse(checkpoint.Variables)!.AsObject()
        };

    /// <summary>
    /// Reads a variable by a dotted path such as <c>trigger.value</c>; a number segment indexes a list.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The value, or <see langword="null"/> when nothing is there.</returns>
    public JsonNode? Variable(string path)
    {
        JsonNode? current = _variables;
        foreach (var segment in path.Split('.'))
        {
            current = current switch
            {
                JsonObject node => node[segment],
                JsonArray list when int.TryParse(segment, CultureInfo.InvariantCulture, out var index) &&
                                    index >= 0 && index < list.Count => list[index],
                _ => null
            };
        }

        return current;
    }

    /// <summary>
    /// Completes the current Node, keeping its value in the Node's variable, and moves on to its next Node. A wait
    /// for a condition or an Event completes this way when it holds or arrives before its timeout.
    /// </summary>
    /// <param name="output">The Node's value, such as a reading, an Action's result or an Event.</param>
    /// <param name="now">The current time.</param>
    public void Complete(JsonNode? output, DateTimeOffset now)
    {
        var node = Current();
        if (node.Into is { } into)
        {
            _variables[into] = output?.DeepClone();
        }

        Advance(node.Next, now);
    }

    /// <summary>
    /// Completes the current branch Node, taking its <c>then</c> edge when its comparison holds and its <c>else</c>
    /// edge otherwise.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <exception cref="InvalidOperationException">The current Node is not a branch.</exception>
    public void Branch(DateTimeOffset now)
    {
        var branch = Current() as BranchNode ?? throw new InvalidOperationException("The Node is not a branch.");
        var value = Variable(branch.From);
        using var reading = JsonDocument.Parse(value?.ToJsonString() ?? "null");
        Advance(value is not null && branch.If.Holds(reading.RootElement) ? branch.Then : branch.Else, now);
    }

    /// <summary>
    /// Fails the current Node: it follows its error edge with the failure in <c>error</c> when it has one, and
    /// otherwise the Flow Execution ends <c>Failed</c>.
    /// </summary>
    /// <param name="code">What went wrong, such as <c>Timeout</c> or <c>ReferenceMissing</c>.</param>
    /// <param name="message">What went wrong, for Engineers.</param>
    /// <param name="attempts">How many attempts the Node made.</param>
    /// <param name="now">The current time.</param>
    public void Fail(string code, string message, int attempts, DateTimeOffset now)
    {
        var node = Current();
        if (node.Error is not { } edge)
        {
            Finish(FlowExecutionState.Failed, new FlowExecutionError(code, message, node.Id, attempts), now);

            return;
        }

        _variables["error"] = new JsonObject
        {
            ["node"] = node.Id,
            ["code"] = code,
            ["message"] = message,
            ["attempts"] = attempts
        };
        Advance(edge, now);
    }

    /// <summary>
    /// Starts the current wait Node: a wait for a duration waits until it has passed, and a wait for a condition or
    /// an Event waits until it times out.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <exception cref="InvalidOperationException">The current Node is not a wait.</exception>
    public void Wait(DateTimeOffset now)
    {
        var wait = Current() as WaitNode ?? throw new InvalidOperationException("The Node is not a wait.");
        Deadline = now + (wait.For ?? wait.Timeout!.Value);
        State = FlowExecutionState.Waiting;
        UpdatedAt = now;
    }

    /// <summary>
    /// Ends the current wait once its deadline has passed. A wait for a duration continues and records how late it
    /// is; one for a condition or Event has timed out. Past <c>maxLateness</c>, or on a timeout, it takes its
    /// <c>timedOut</c> edge, then its error edge, and otherwise fails.
    /// </summary>
    /// <param name="now">The current time, at or after the deadline.</param>
    /// <exception cref="InvalidOperationException">The Flow Execution is not waiting.</exception>
    public void Elapse(DateTimeOffset now)
    {
        var wait = Current() as WaitNode;
        if (wait is null || Deadline is not { } deadline)
        {
            throw new InvalidOperationException("The Flow Execution is not waiting.");
        }

        var lateness = now - deadline;
        Lateness = lateness > TimeSpan.Zero ? lateness : null;
        var tooLate = wait.MaxLateness is { } allowed && lateness > allowed;
        if (wait.For is not null && !tooLate)
        {
            Advance(wait.Next, now);

            return;
        }

        var (code, message) = tooLate
            ? ("TooLate", $"The wait resumed {lateness.TotalSeconds:0.#}s past its deadline.")
            : ("Timeout", "The wait timed out.");
        if (wait.TimedOut is { } edge)
        {
            Advance(edge, now);
        }
        else
        {
            Fail(code, message, 1, now);
        }
    }

    /// <summary>
    /// Asks the Flow Execution to stop at its next checkpoint.
    /// </summary>
    public void RequestCancel() => CancelRequested = !IsFinished;

    /// <summary>
    /// Stops the Flow Execution at once, as <c>Cancelled</c>; one already finished stays as it was.
    /// </summary>
    /// <param name="now">The current time.</param>
    public void Cancel(DateTimeOffset now)
    {
        if (!IsFinished)
        {
            Finish(FlowExecutionState.Cancelled, null, now);
        }
    }

    private FlowNode Current() =>
        Node ?? throw new InvalidOperationException("The Flow Execution is not at a Node.");

    private void Advance(string? target, DateTimeOffset now)
    {
        var from = CurrentNode!;
        _visits[from] = _visits.GetValueOrDefault(from) + 1;
        Steps++;
        Deadline = null;
        var limits = Graph.Limits;
        if (Encoding.UTF8.GetByteCount(_variables.ToJsonString()) > limits.VariableBytes)
        {
            Finish(
                FlowExecutionState.Failed,
                new FlowExecutionError(
                    "VariablesTooLarge",
                    $"The variables outgrew {limits.VariableBytes} bytes.",
                    from,
                    1),
                now);
        }
        else if (target is null)
        {
            Finish(FlowExecutionState.Succeeded, null, now);
        }
        else if (Steps >= limits.Steps)
        {
            Finish(
                FlowExecutionState.Failed,
                new FlowExecutionError("StepLimit", $"The Flow Execution completed {limits.Steps} Nodes.", from, 1),
                now);
        }
        else
        {
            CurrentNode = target;
            State = FlowExecutionState.Running;
            UpdatedAt = now;
        }
    }

    private void Finish(FlowExecutionState state, FlowExecutionError? error, DateTimeOffset now)
    {
        State = state;
        Error = error;
        CurrentNode = state == FlowExecutionState.Failed ? CurrentNode : null;
        Deadline = null;
        CancelRequested = false;
        UpdatedAt = now;
        FinishedAt = now;
    }
}
