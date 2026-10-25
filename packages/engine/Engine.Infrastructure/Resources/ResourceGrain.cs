using System.Text.Json;

namespace Engine.Infrastructure.Resources;

/// <summary>
/// What every Resource grain shares: the sequence-number gate of ADR-0014, the persisted state and the debounced
/// reporting of its Status.
/// </summary>
internal abstract class ResourceGrain(
    IPersistentState<ResourceState> state,
    IStatusReporter reporter,
    TimeSpan statusInterval,
    string kind) : Grain
{
    private string? _lastStatus;
    private DateTimeOffset _lastReport = DateTimeOffset.MinValue;
    private IDisposable? _pendingReport;
    protected ResourceState State => state.State;
    protected string Namespace => this.GetPrimaryKeyString().Split('/', 2)[0];
    protected string ResourceName => this.GetPrimaryKeyString().Split('/', 2)[1];
    protected bool IsLive => State.Spec is not null && !State.Deleted;
    protected JsonElement Spec => JsonDocument.Parse(State.Spec!).RootElement;
    protected abstract JsonElement BuildStatus();

    protected async Task<bool> AcceptAsync(Guid uid, long generation, long seq, string spec)
    {
        if (seq <= State.Seq)
        {
            return false;
        }

        state.State = new ResourceState
        {
            Uid = uid,
            Generation = generation,
            Seq = seq,
            Spec = spec,
            StatusSeq = uid == State.Uid ? State.StatusSeq : 0
        };
        await state.WriteStateAsync();

        return true;
    }

    protected async Task<bool> TombstoneAsync(Guid uid, long seq)
    {
        if (seq <= State.Seq)
        {
            return false;
        }

        _pendingReport?.Dispose();
        state.State = new ResourceState
        {
            Uid = uid,
            Generation = State.Generation,
            Seq = seq,
            Deleted = true
        };
        await state.WriteStateAsync();

        return true;
    }

    protected Task ReportStatusAsync()
    {
        if (!IsLive)
        {
            return Task.CompletedTask;
        }

        var wait = _lastReport + statusInterval - DateTimeOffset.UtcNow;
        if (wait <= TimeSpan.Zero)
        {
            return FlushStatusAsync();
        }

        _pendingReport ??= this.RegisterGrainTimer(
            async () =>
            {
                _pendingReport?.Dispose();
                _pendingReport = null;
                await FlushStatusAsync();
            },
            wait,
            Timeout.InfiniteTimeSpan);

        return Task.CompletedTask;
    }

    private async Task FlushStatusAsync()
    {
        if (!IsLive)
        {
            return;
        }

        var status = BuildStatus();
        var text = $"{State.Generation}:{status.GetRawText()}";
        if (text == _lastStatus)
        {
            return;
        }

        _lastStatus = text;
        _lastReport = DateTimeOffset.UtcNow;
        State.StatusSeq++;
        await state.WriteStateAsync();
        await reporter.ReportAsync(Namespace, kind, ResourceName, State.Uid, State.Generation, State.StatusSeq, status);
    }
}
