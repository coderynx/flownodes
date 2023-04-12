using Flownodes.Sdk.Entities;
using Flownodes.Shared.Eventing;
using Orleans.Runtime;

namespace Flownodes.Worker.Eventing;

[GrainType(FlownodesEntityNames.EventBook)]
internal sealed class EventBookGrain : Grain, IEventBookGrain
{
    private readonly ILogger<EventBookGrain> _logger;
    private readonly IPersistentState<HashSet<EventRegistration>> _store;

    public EventBookGrain(ILogger<EventBookGrain> logger,
        [PersistentState("event_book_store")] IPersistentState<HashSet<EventRegistration>> store)
    {
        _logger = logger;
        _store = store;
    }

    private FlownodesId Id => (FlownodesId)this.GetPrimaryKeyString();

    public async ValueTask<EventRegistration> RegisterEventAsync(EventKind kind, FlownodesId targetEntityId)
    {
        var registration = new EventRegistration(DateTime.Now, kind, targetEntityId);

        _store.State.Add(registration);
        await _store.WriteStateAsync();

        _logger.LogInformation("Registered event {@EventRegistration}", registration);
        return registration;
    }

    public ValueTask<HashSet<EventRegistration>> GetEvents()
    {
        return ValueTask.FromResult(_store.State);
    }

    public ValueTask<FlownodesId> GetId()
    {
        return ValueTask.FromResult(Id);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activated {@EventBookGrainId}", Id);
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivated {@EventBookGrainId} for reason {@DeactivationReason}", Id,
            reason.Description);
        return Task.CompletedTask;
    }
}