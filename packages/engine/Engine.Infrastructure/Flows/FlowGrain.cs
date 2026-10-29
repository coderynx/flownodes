using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.Domain.Flows;
using Engine.Infrastructure.Assets;
using Engine.Infrastructure.Connections;
using Engine.Infrastructure.Devices;
using Engine.Infrastructure.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Infrastructure.Flows;

internal sealed class FlowGrain(
    [PersistentState("flow")] IPersistentState<ResourceState> state,
    [PersistentState("flowstate")] IPersistentState<FlowState> flowState,
    IFlowExecutions executions,
    IStatusReporter reporter,
    IOptions<FlowGrainOptions> options,
    ILogger<FlowGrain> logger) : ResourceGrain(state, reporter, options.Value.StatusInterval, "Flow"), IFlowGrain
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan LongestTimer = TimeSpan.FromHours(12);
    private readonly HashSet<Guid> _active = [];
    private readonly Dictionary<string, IDisposable> _schedules = [];
    private readonly HashSet<(string Kind, string Name)> _watched = [];
    private readonly List<string> _missing = [];
    private FlowGraph? _graph;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (IsLive)
        {
            await SyncAsync();
        }
    }

    public async Task ApplyAsync(Guid uid, long generation, long seq, string spec)
    {
        var previousUid = State.Uid;
        if (!await AcceptAsync(uid, generation, seq, spec))
        {
            return;
        }

        _graph = null;
        StopSchedules();
        if (previousUid != uid)
        {
            if (previousUid != Guid.Empty)
            {
                await CancelExecutionsAsync(previousUid);
            }

            _active.Clear();
            flowState.State = new FlowState();
        }
        else
        {
            Forget();
        }

        await flowState.WriteStateAsync();
        await SyncAsync();
    }

    public async Task DeleteAsync(Guid uid, long seq)
    {
        if (!await TombstoneAsync(uid, seq))
        {
            return;
        }

        StopSchedules();
        await UnwatchAsync(_watched.ToList());
        await CancelExecutionsAsync(uid);
        _active.Clear();
        await flowState.ClearStateAsync();
        DeactivateOnIdle();
    }

    public async Task PingAsync()
    {
        if (!IsLive)
        {
            return;
        }

        await SyncAsync();
        foreach (var id in _active.ToList())
        {
            try
            {
                await Execution(id).PingAsync();
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                logger.LogWarning(exception, "Flow {Name} could not ping Flow Execution {Id}", ResourceName, id);
            }
        }
    }

    public async Task<TriggerResult> TriggerAsync(string? trigger, string payload)
    {
        if (!IsLive)
        {
            return new TriggerResult(TriggerOutcome.FlowMissing, null);
        }

        var triggers = Graph.Triggers;
        var only = triggers.Count == 1 ? triggers[0] : null;
        var chosen = trigger is null ? only : triggers.FirstOrDefault(candidate => candidate.Id == trigger);
        if (chosen is null)
        {
            return new TriggerResult(TriggerOutcome.TriggerUnknown, null);
        }

        return await FireAsync(chosen.Id, JsonNode.Parse(payload));
    }

    public async Task<int?> CancelAllAsync()
    {
        if (!IsLive)
        {
            return null;
        }

        flowState.State.Queue.Clear();
        await flowState.WriteStateAsync();
        var cancelled = await CancelExecutionsAsync(State.Uid);
        await ReportStatusAsync();

        return cancelled;
    }

    public async Task ExecutionFinishedAsync(Guid flowUid, Guid execution, string state, DateTimeOffset finishedAt)
    {
        if (!IsLive || flowUid != State.Uid)
        {
            return;
        }

        _active.Remove(execution);
        await KeepNewestAsync(new LastResult(execution, state, finishedAt));

        await StartQueuedAsync();
        await ReportStatusAsync();
    }

    public async Task DataPointChangedAsync(string kind, string resource, DataPointSnapshot snapshot)
    {
        if (IsLive)
        {
            await OnReadingAsync(kind, resource, snapshot, false);
        }
    }

    public async Task EventReportedAsync(
        string kind,
        string resource,
        string name,
        string payload,
        DateTimeOffset timestamp)
    {
        if (!IsLive)
        {
            return;
        }

        var reported = new MemberRef(Enum.Parse<MemberOwner>(kind), resource, name);
        foreach (var trigger in Graph.Triggers.Where(candidate => candidate.Event == reported))
        {
            await FireAsync(
                trigger.Id,
                new JsonObject
                {
                    ["event"] = name,
                    ["payload"] = JsonNode.Parse(payload),
                    ["timestamp"] = timestamp
                });
        }
    }

    protected override JsonElement BuildStatus()
    {
        var ready = _missing.Count == 0;

        return JsonSerializer.SerializeToElement(
            new
            {
                conditions = new[]
                {
                    new
                    {
                        type = "Ready",
                        status = ready ? "True" : "False",
                        reason = ready ? "Resolved" : "ReferenceMissing",
                        message = ready ? null : $"Not there: {string.Join(", ", _missing)}."
                    }
                },
                activeExecutions = _active.Count,
                lastResult = flowState.State.Last is { } last
                    ? new { execution = last.Execution, state = last.State.ToLowerInvariant(), last.FinishedAt }
                    : null
            },
            Web);
    }

    private FlowGraph Graph => _graph ??= FlowGraph.Create(Spec);

    private IFlowExecutionGrain Execution(Guid id) =>
        GrainFactory.GetGrain<IFlowExecutionGrain>($"{Namespace}/{ResourceName}/{id}");

    private async Task SyncAsync()
    {
        _active.Clear();
        _active.UnionWith(await executions.ListActiveAsync(Namespace, ResourceName, State.Uid));
        if (await executions.FindLastResultAsync(Namespace, ResourceName, State.Uid) is { } last)
        {
            await KeepNewestAsync(last);
        }

        await WatchAsync();
        await StartSchedulesAsync();
        await StartQueuedAsync();
        await ReportStatusAsync();
    }

    private async Task KeepNewestAsync(LastResult candidate)
    {
        if (flowState.State.Last == candidate || flowState.State.Last?.FinishedAt > candidate.FinishedAt)
        {
            return;
        }

        flowState.State.Last = candidate;
        await flowState.WriteStateAsync();
    }

    private void Forget()
    {
        var triggers = Graph.Triggers.Select(trigger => trigger.Id).ToHashSet();
        foreach (var id in flowState.State.Conditions.Keys.Where(id => !triggers.Contains(id)).ToList())
        {
            flowState.State.Conditions.Remove(id);
        }

        foreach (var id in flowState.State.Scheduled.Keys.Where(id => !triggers.Contains(id)).ToList())
        {
            flowState.State.Scheduled.Remove(id);
        }
    }

    private async Task WatchAsync()
    {
        _missing.Clear();
        var wanted = new HashSet<(string Kind, string Name)>();
        var me = this.AsReference<IFlowWatcher>();
        foreach (var trigger in Graph.Triggers)
        {
            try
            {
                if (trigger.DataPoint is { } point)
                {
                    var kind = point.Owner.ToString();
                    wanted.Add((kind, point.Resource));
                    var result = point.Owner == DataPointOwner.Asset
                        ? await Asset(point.Resource).WatchDataPointAsync(me, point.Name)
                        : await Device(point.Resource).WatchDataPointAsync(me, point.Name);
                    if (!result.Exists)
                    {
                        _missing.Add($"Data Point '{point.Name}' of {kind} '{point.Resource}'");
                    }
                    else if (trigger.When is not null && result.Snapshot is { } snapshot)
                    {
                        await OnReadingAsync(kind, point.Resource, snapshot, true);
                    }
                }

                if (trigger.Event is { } wantedEvent)
                {
                    var owner = wantedEvent.Owner.ToString();
                    wanted.Add((owner, wantedEvent.Resource));
                    if (!await WatchEventAsync(wantedEvent, me))
                    {
                        _missing.Add($"Event '{wantedEvent.Name}' of {owner} '{wantedEvent.Resource}'");
                    }
                }
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                logger.LogWarning(exception, "Flow {Name} could not watch trigger {Trigger}", ResourceName, trigger.Id);
            }
        }

        await UnwatchAsync([.. _watched.Where(source => !wanted.Contains(source))]);
        _watched.Clear();
        _watched.UnionWith(wanted);
    }

    private async Task UnwatchAsync(IReadOnlyList<(string Kind, string Name)> sources)
    {
        var me = this.AsReference<IFlowWatcher>();
        foreach (var (kind, name) in sources)
        {
            try
            {
                await (kind switch
                {
                    nameof(DataPointOwner.Asset) => Asset(name).UnwatchAsync(me),
                    nameof(MemberOwner.Connection) => Connection(name).UnwatchAsync(me),
                    _ => Device(name).UnwatchAsync(me)
                });
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                logger.LogWarning(
                    exception,
                    "Flow {Name} could not stop watching {Kind} {Source}",
                    ResourceName,
                    kind,
                    name);
            }

            _watched.Remove((kind, name));
        }
    }

    private IAssetGrain Asset(string name) => GrainFactory.GetGrain<IAssetGrain>($"{Namespace}/{name}");
    private IDeviceGrain Device(string name) => GrainFactory.GetGrain<IDeviceGrain>($"{Namespace}/{name}");

    private IConnectionGrain Connection(string name) =>
        GrainFactory.GetGrain<IConnectionGrain>($"{Namespace}/{name}");

    private Task<bool> WatchEventAsync(MemberRef wanted, IFlowWatcher me) =>
        wanted.Owner == MemberOwner.Connection
            ? Connection(wanted.Resource).WatchEventAsync(me, wanted.Name)
            : Device(wanted.Resource).WatchEventAsync(me, wanted.Name);

    private async Task OnReadingAsync(string kind, string resource, DataPointSnapshot snapshot, bool initial)
    {
        var triggers = Graph.Triggers.Where(trigger =>
            trigger.DataPoint is { } point && point.Owner.ToString() == kind && point.Resource == resource &&
            point.Name == snapshot.Name);
        foreach (var trigger in triggers)
        {
            if (trigger.When is not { } condition)
            {
                if (!initial)
                {
                    await FireAsync(trigger.Id, Readings.From(snapshot));
                }

                continue;
            }

            if (!Readings.IsUsable(snapshot))
            {
                continue;
            }

            using var value = JsonDocument.Parse(snapshot.Value!);
            var holds = condition.Holds(value.RootElement);
            var held = flowState.State.Conditions.GetValueOrDefault(trigger.Id);
            if (holds == held)
            {
                continue;
            }

            flowState.State.Conditions[trigger.Id] = holds;
            await flowState.WriteStateAsync();
            if (holds)
            {
                await FireAsync(trigger.Id, Readings.From(snapshot));
            }
        }
    }

    private async Task<TriggerResult> FireAsync(string trigger, JsonNode? payload)
    {
        var admission = Graph.Admit(_active.Count, flowState.State.Queue.Count);
        switch (admission)
        {
            case FlowAdmission.Queue:
                flowState.State.Queue.Add(new QueuedTrigger(trigger, payload?.ToJsonString()));
                await flowState.WriteStateAsync();
                await ReportStatusAsync();

                return new TriggerResult(TriggerOutcome.Queued, null);
            case FlowAdmission.Drop:
                var dropped = await DropAsync(
                    trigger,
                    "Busy",
                    $"Execution Mode {Graph.Mode.ToString().ToLowerInvariant()} refused the trigger.");

                return new TriggerResult(TriggerOutcome.Dropped, dropped);
            case FlowAdmission.Restart:
                await CancelExecutionsAsync(State.Uid);
                break;
        }

        return new TriggerResult(TriggerOutcome.Started, await StartAsync(trigger, payload));
    }

    private async Task<Guid> StartAsync(string trigger, JsonNode? payload)
    {
        var execution = FlowExecution.Start(
            Guid.CreateVersion7(),
            Namespace,
            ResourceName,
            State.Uid,
            State.Generation,
            State.Spec!,
            trigger,
            payload,
            DateTimeOffset.UtcNow);
        await executions.SaveAsync(execution);
        if (execution.IsFinished)
        {
            flowState.State.Last = new LastResult(
                execution.Id,
                execution.State.ToString(),
                execution.FinishedAt!.Value);
            await flowState.WriteStateAsync();
        }
        else
        {
            _active.Add(execution.Id);
            await Execution(execution.Id).PingAsync();
        }

        await ReportStatusAsync();

        return execution.Id;
    }

    private async Task<Guid> DropAsync(string trigger, string reason, string message)
    {
        var dropped = FlowExecution.Drop(
            Guid.CreateVersion7(),
            Namespace,
            ResourceName,
            State.Uid,
            State.Generation,
            State.Spec!,
            trigger,
            reason,
            message,
            DateTimeOffset.UtcNow);
        await executions.SaveAsync(dropped);
        logger.LogInformation("Flow {Name} dropped trigger {Trigger}: {Message}", ResourceName, trigger, message);

        return dropped.Id;
    }

    private async Task StartQueuedAsync()
    {
        while (flowState.State.Queue.Count > 0 && Graph.Admit(_active.Count, 0) == FlowAdmission.Start)
        {
            var next = flowState.State.Queue[0];
            flowState.State.Queue.RemoveAt(0);
            await flowState.WriteStateAsync();
            if (Graph.Node(next.Trigger) is TriggerNode)
            {
                await StartAsync(next.Trigger, next.Payload is null ? null : JsonNode.Parse(next.Payload));
            }
        }
    }

    private async Task<int> CancelExecutionsAsync(Guid uid)
    {
        var ids = await executions.RequestCancelAsync(Namespace, ResourceName, uid);
        foreach (var id in ids)
        {
            try
            {
                await Execution(id).CancelAsync();
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                logger.LogWarning(exception, "Flow {Name} could not cancel Flow Execution {Id}", ResourceName, id);
            }
        }

        return ids.Count;
    }

    private async Task StartSchedulesAsync()
    {
        var changed = false;
        foreach (var trigger in Graph.Triggers.Where(trigger => trigger.Schedule is not null))
        {
            changed |= flowState.State.Scheduled.TryAdd(trigger.Id, DateTimeOffset.UtcNow);
            if (!_schedules.ContainsKey(trigger.Id))
            {
                Arm(trigger);
            }
        }

        if (changed)
        {
            await flowState.WriteStateAsync();
        }
    }

    private void Arm(TriggerNode trigger)
    {
        var due = trigger.Schedule!.Cron.Next(flowState.State.Scheduled[trigger.Id], Zone(trigger.Schedule));
        var wait = Math.Clamp((due - DateTimeOffset.UtcNow).Ticks, TimeSpan.TicksPerMillisecond, LongestTimer.Ticks);
        _schedules[trigger.Id] = this.RegisterGrainTimer(
            () => OnScheduleAsync(trigger.Id),
            TimeSpan.FromTicks(wait),
            Timeout.InfiniteTimeSpan);
    }

    private async Task OnScheduleAsync(string id)
    {
        _schedules.Remove(id, out var timer);
        timer?.Dispose();
        if (!IsLive || Graph.Node(id) is not TriggerNode { Schedule: { } schedule } trigger)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var zone = Zone(schedule);
        var due = schedule.Cron.Next(flowState.State.Scheduled[id], zone);
        if (due > now)
        {
            Arm(trigger);

            return;
        }

        for (var later = schedule.Cron.Next(due, zone); later <= now; later = schedule.Cron.Next(due, zone))
        {
            due = later;
        }

        flowState.State.Scheduled[id] = due;
        await flowState.WriteStateAsync();
        var lateness = now - due;
        if (schedule.MaxLateness is { } allowed && lateness > allowed)
        {
            await DropAsync(id, "TooLate", $"The fire due at {due:O} came {lateness.TotalSeconds:0.#}s late.");
        }
        else
        {
            await FireAsync(
                id,
                new JsonObject
                {
                    ["scheduledAt"] = due,
                    ["latenessMs"] = (long)lateness.TotalMilliseconds
                });
        }

        Arm(trigger);
    }

    private TimeZoneInfo Zone(ScheduleTrigger schedule) =>
        TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZone ?? options.Value.TimeZone);

    private void StopSchedules()
    {
        foreach (var timer in _schedules.Values)
        {
            timer.Dispose();
        }

        _schedules.Clear();
    }
}
