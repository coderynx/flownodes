using Microsoft.Extensions.Options;

namespace Engine.Infrastructure.Namespaces;

internal sealed class NamespaceGrain(
    [PersistentState("namespace")] IPersistentState<NamespaceState> state,
    IRegistryRequests registry,
    IOptions<NamespaceGrainOptions> options) : Grain, INamespaceGrain, IRemindable
{
    private const string ReminderName = "keep-alive";
    private IDisposable? _resyncTimer;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (!state.State.Deleted)
        {
            StartResyncTimer();
            await registry.ResyncNamespaceAsync(this.GetPrimaryKeyString(), cancellationToken);
        }
    }

    public async Task ApplyAsync(Guid uid, long seq)
    {
        var current = state.State;
        var isStale = current.Deleted ? current.Uid == uid : seq <= current.Seq;
        if (isStale)
        {
            return;
        }

        var previous = current.Deleted ? 0 : current.Seq;
        var wasDeleted = current.Deleted;
        state.State = new NamespaceState
        {
            Uid = uid,
            Seq = seq
        };
        await state.WriteStateAsync();

        await this.RegisterOrUpdateReminder(ReminderName, options.Value.ReminderPeriod, options.Value.ReminderPeriod);
        if (wasDeleted)
        {
            StartResyncTimer();
        }

        if (seq > previous + 1)
        {
            await registry.ResyncNamespaceAsync(this.GetPrimaryKeyString(), CancellationToken.None);
        }
    }

    public async Task DeleteAsync(Guid uid, long seq)
    {
        if (state.State.Deleted || seq <= state.State.Seq)
        {
            return;
        }

        state.State = new NamespaceState
        {
            Uid = uid,
            Seq = seq,
            Deleted = true
        };
        await state.WriteStateAsync();

        _resyncTimer?.Dispose();
        _resyncTimer = null;

        var reminder = await this.GetReminder(ReminderName);
        if (reminder is not null)
        {
            await this.UnregisterReminder(reminder);
        }

        DeactivateOnIdle();
    }

    public Task ReceiveReminder(string reminderName, TickStatus status) => Task.CompletedTask;

    private void StartResyncTimer() =>
        _resyncTimer ??= this.RegisterGrainTimer(
            token => registry.ResyncNamespaceAsync(this.GetPrimaryKeyString(), token),
            options.Value.ResyncPeriod,
            options.Value.ResyncPeriod);
}
