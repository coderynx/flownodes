using Flownodes.Shared.Resourcing.Grains;
using Orleans.EventSourcing;
using Orleans.Providers;

namespace Flownodes.Worker.Resourcing;

internal interface IJournaledStoreGrainEvent
{
}

internal sealed record UpdateJournaledStoreGrain<TState>(TState State) : IJournaledStoreGrainEvent where TState : class;

[GenerateSerializer]
internal sealed class JournaledStoreGrainState<TState> where TState : class, new()
{
    [Id(0)] public TState State { get; private set; } = new();
    [Id(1)] public DateTime? UpdatedAt { get; private set; }

    public void Update(TState state)
    {
        State = state;
        UpdatedAt = DateTime.Now;
    }
}

[LogConsistencyProvider]
[StorageProvider]
internal sealed class JournaledStoreGrain<TState> :
    JournaledGrain<JournaledStoreGrainState<TState>, IJournaledStoreGrainEvent>,
    IJournaledStoreGrain<TState> where TState : class, new()
{
    private readonly ILogger<JournaledStoreGrain<TState>> _logger;

    public JournaledStoreGrain(ILogger<JournaledStoreGrain<TState>> logger)
    {
        _logger = logger;
    }

    private string Id => this.GetPrimaryKeyString();

    public async Task UpdateAsync(TState state)
    {
        var @event = new UpdateJournaledStoreGrain<TState>(state);
        await RaiseConditionalEvent(@event);

        _logger.LogInformation("Updated state of JournaledStateGrain {@JournaledStateGrainId}", Id);
    }

    public ValueTask<TState> Get()
    {
        return ValueTask.FromResult(State.State);
    }

    public ValueTask<DateTime?> GetUpdatedAt()
    {
        return ValueTask.FromResult(State.UpdatedAt);
    }

    public async Task ClearAsync()
    {
        var @event = new UpdateJournaledStoreGrain<TState>(new TState());
        await RaiseConditionalEvent(@event);

        _logger.LogInformation("Cleared state of JournaledStateGrain {@JournaledStateGrainId}", Id);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activated JournaledStoreGrain {@JournaledStoreGrainId}", Id);
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Deactivated JournaledStoreGrain {@JournaledStoreGrainId} for reason {@DeactivationReason}",
            Id,
            reason.Description);
        return Task.CompletedTask;
    }

    protected override void TransitionState(JournaledStoreGrainState<TState> state, IJournaledStoreGrainEvent @event)
    {
        switch (@event)
        {
            case UpdateJournaledStoreGrain<TState> update:
                State.Update(update.State);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(@event));
        }
    }
}