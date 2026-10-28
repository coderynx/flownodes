using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.Domain.Flows;
using Engine.Infrastructure.Assets;
using Engine.Infrastructure.Connections;
using Engine.Infrastructure.Devices;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Engine.Infrastructure.Flows;

[Reentrant]
internal sealed class FlowExecutionGrain(IFlowExecutions executions, ILogger<FlowExecutionGrain> logger)
    : Grain, IFlowExecutionGrain
{
    private const string NotReady = "ConnectionNotReady";
    private static readonly TimeSpan NotReadyPoll = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan LongestTimer = TimeSpan.FromHours(12);
    private FlowExecution? _execution;
    private bool _running;
    private bool _armed;
    private IDisposable? _deadline;
    private (string Kind, string Name)? _watching;

    public async Task PingAsync()
    {
        if (await LoadAsync() is { IsFinished: false })
        {
            _ = RunAsync();
        }
    }

    public async Task<bool> CancelAsync()
    {
        if (await LoadAsync() is not { IsFinished: false } execution)
        {
            return false;
        }

        execution.RequestCancel();
        if (!_running)
        {
            await RunAsync();
        }

        return true;
    }

    public async Task DataPointChangedAsync(string kind, string resource, DataPointSnapshot snapshot)
    {
        if (_execution is not { State: FlowExecutionState.Waiting, Node: WaitNode { Until: { } until } })
        {
            return;
        }

        var watched = until.DataPoint;
        var matches = watched.Owner.ToString() == kind && watched.Resource == resource && watched.Name == snapshot.Name;
        if (matches && Satisfies(until, snapshot))
        {
            await ResumeAsync(execution => execution.Satisfy(Readings.From(snapshot), DateTimeOffset.UtcNow));
        }
    }

    public async Task EventReportedAsync(string device, string name, string payload, DateTimeOffset timestamp)
    {
        if (_execution is not { State: FlowExecutionState.Waiting, Node: WaitNode { Event: { } wanted } })
        {
            return;
        }

        if (wanted.Device == device && wanted.Name == name)
        {
            var arrived = new JsonObject
            {
                ["event"] = name,
                ["payload"] = JsonNode.Parse(payload),
                ["timestamp"] = timestamp
            };
            await ResumeAsync(execution => execution.Satisfy(arrived, DateTimeOffset.UtcNow));
        }
    }

    private Guid Id => Guid.Parse(this.GetPrimaryKeyString().Split('/')[2]);
    private string Namespace => this.GetPrimaryKeyString().Split('/')[0];

    private async Task<FlowExecution?> LoadAsync() => _execution ??= await executions.FindAsync(Id);

    private async Task ResumeAsync(Action<FlowExecution> resume)
    {
        if (_running || _execution is not { State: FlowExecutionState.Waiting } execution)
        {
            return;
        }

        Disarm();
        resume(execution);
        await SaveAsync(execution);
        await RunAsync();
    }

    private async Task RunAsync()
    {
        if (_running || _execution is not { } execution)
        {
            return;
        }

        _running = true;
        try
        {
            while (!execution.IsFinished)
            {
                if (execution.CancelRequested)
                {
                    Disarm();
                    execution.Cancel(DateTimeOffset.UtcNow);
                    await SaveAsync(execution);
                }
                else if (execution.State == FlowExecutionState.Waiting)
                {
                    if (await ParkAsync(execution))
                    {
                        return;
                    }
                }
                else
                {
                    await StepAsync(execution);
                    await SaveAsync(execution);
                }
            }

            Disarm();
            await UnwatchAsync();
            GrainFactory.GetGrain<IFlowGrain>($"{Namespace}/{execution.Flow}")
                .ExecutionFinishedAsync(
                    execution.FlowUid,
                    execution.Id,
                    execution.State.ToString(),
                    execution.FinishedAt!.Value)
                .Ignore();
            DeactivateOnIdle();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            logger.LogWarning(exception, "Flow Execution {Id} stopped and resumes on its next ping", execution.Id);
            _execution = null;
        }
        finally
        {
            _running = false;
        }
    }

    private async Task SaveAsync(FlowExecution execution)
    {
        if (await executions.SaveAsync(execution))
        {
            execution.RequestCancel();
        }
    }

    private async Task StepAsync(FlowExecution execution)
    {
        switch (execution.Node)
        {
            case ReadNode read:
                await CallAsync(execution, read.Policy, () => ReadAsync(read.DataPoint));
                break;
            case WriteNode write:
                var value = write.Value ?? execution.Variable(write.From!)?.ToJsonString();
                if (value is null)
                {
                    execution.Fail("VariableMissing", $"Variable '{write.From}' is not set.", 1, DateTimeOffset.UtcNow);
                }
                else
                {
                    await CallAsync(execution, write.Policy, () => WriteAsync(write.DataPoint, value));
                }

                break;
            case InvokeNode invoke:
                var parameters = invoke.From is { } from
                    ? execution.Variable(from)?.ToJsonString() ?? "{}"
                    : invoke.Parameters;
                var invocation = execution.InvocationId;
                await CallAsync(
                    execution,
                    invoke.Policy,
                    () => Device(invoke.Action.Device).InvokeAsync(invoke.Action.Name, parameters, invocation));
                break;
            case BranchNode:
                execution.Branch(DateTimeOffset.UtcNow);
                break;
            case WaitNode:
                execution.Wait(DateTimeOffset.UtcNow);
                break;
            default:
                execution.Fail(
                    "ReferenceMissing",
                    "The Flow Execution is at no runnable Node.",
                    1,
                    DateTimeOffset.UtcNow);
                break;
        }
    }

    private static async Task CallAsync(FlowExecution execution, CallPolicy policy, Func<Task<CallResult>> call)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            var result = await AttemptAsync(policy.Timeout, call);
            if (result.Ok)
            {
                execution.Complete(result.Json is null ? null : JsonNode.Parse(result.Json), DateTimeOffset.UtcNow);

                return;
            }

            var code = result.Code == "NotFound" ? "ReferenceMissing" : result.Code;
            if (code == "ReferenceMissing" || attempt > policy.Retries)
            {
                execution.Fail(code, result.Message ?? code, attempt, DateTimeOffset.UtcNow);

                return;
            }

            await Task.Delay(policy.RetryDelay);
        }
    }

    private static async Task<CallResult> AttemptAsync(TimeSpan timeout, Func<Task<CallResult>> call)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        try
        {
            var result = await call().WaitAsync(timeout);
            while (result.Code == NotReady && DateTimeOffset.UtcNow + NotReadyPoll < deadline)
            {
                await Task.Delay(NotReadyPoll);
                result = await call().WaitAsync(deadline - DateTimeOffset.UtcNow);
            }

            return result;
        }
        catch (TimeoutException)
        {
            return CallResult.Failure("Timeout", $"The Node took longer than {timeout.TotalSeconds:0.#}s.");
        }
    }

    private async Task<CallResult> ReadAsync(DataPointRef target)
    {
        var points = target.Owner == DataPointOwner.Asset
            ? await Asset(target.Resource).GetDataPointsAsync()
            : await Device(target.Resource).GetDataPointsAsync();
        var point = points?.FirstOrDefault(candidate => candidate.Name == target.Name);

        return point is null
            ? CallResult.Failure("NotFound", $"{target.Owner} '{target.Resource}' has no Data Point '{target.Name}'.")
            : CallResult.Success(Readings.From(point).ToJsonString());
    }

    private Task<CallResult> WriteAsync(DataPointRef target, string value) =>
        target.Owner == DataPointOwner.Asset
            ? Asset(target.Resource).WriteAsync(target.Name, value)
            : Device(target.Resource).WriteAsync(target.Name, value);

    private async Task<bool> ParkAsync(FlowExecution execution)
    {
        var now = DateTimeOffset.UtcNow;
        if (execution.Deadline!.Value <= now)
        {
            Disarm();
            execution.Elapse(now);
            await SaveAsync(execution);

            return false;
        }

        if (!_armed)
        {
            var wait = execution.Deadline.Value - now;
            _armed = true;
            _deadline = this.RegisterGrainTimer(
                OnDeadlineAsync,
                wait < LongestTimer ? wait : LongestTimer,
                Timeout.InfiniteTimeSpan);
        }

        if (await WatchAsync(execution) is not { } resolve)
        {
            return true;
        }

        Disarm();
        resolve(execution);
        await SaveAsync(execution);

        return false;
    }

    private async Task OnDeadlineAsync()
    {
        Disarm();
        await RunAsync();
    }

    private async Task<Action<FlowExecution>?> WatchAsync(FlowExecution execution)
    {
        var me = this.AsReference<IFlowWatcher>();
        switch (execution.Node)
        {
            case WaitNode { Until: { } until }:
                var point = until.DataPoint;
                _watching = (point.Owner.ToString(), point.Resource);
                var result = point.Owner == DataPointOwner.Asset
                    ? await Asset(point.Resource).WatchDataPointAsync(me, point.Name)
                    : await Device(point.Resource).WatchDataPointAsync(me, point.Name);
                if (!result.Exists)
                {
                    return Missing($"{point.Owner} '{point.Resource}' has no Data Point '{point.Name}'.");
                }

                return result.Snapshot is { } snapshot && Satisfies(until, snapshot)
                    ? waiting => waiting.Satisfy(Readings.From(snapshot), DateTimeOffset.UtcNow)
                    : null;
            case WaitNode { Event: { } wanted }:
                _watching = ("Device", wanted.Device);

                return await Device(wanted.Device).WatchEventAsync(me, wanted.Name)
                    ? null
                    : Missing($"Device '{wanted.Device}' has no Event '{wanted.Name}'.");
            default:
                return null;
        }
    }

    private static Action<FlowExecution> Missing(string message) =>
        waiting => waiting.Fail("ReferenceMissing", message, 1, DateTimeOffset.UtcNow);

    private static bool Satisfies(WaitCondition until, DataPointSnapshot snapshot)
    {
        if (!Readings.IsUsable(snapshot))
        {
            return false;
        }

        using var value = JsonDocument.Parse(snapshot.Value!);

        return until.Comparison.Holds(value.RootElement);
    }

    private void Disarm()
    {
        _deadline?.Dispose();
        _deadline = null;
        _armed = false;
    }

    private async Task UnwatchAsync()
    {
        if (_watching is not var (kind, name))
        {
            return;
        }

        _watching = null;
        var me = this.AsReference<IFlowWatcher>();
        try
        {
            await (kind == nameof(DataPointOwner.Asset) ? Asset(name).UnwatchAsync(me) : Device(name).UnwatchAsync(me));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            logger.LogWarning(exception, "Flow Execution {Id} could not stop watching {Kind} {Name}", Id, kind, name);
        }
    }

    private IAssetGrain Asset(string name) => GrainFactory.GetGrain<IAssetGrain>($"{Namespace}/{name}");
    private IDeviceGrain Device(string name) => GrainFactory.GetGrain<IDeviceGrain>($"{Namespace}/{name}");
}
