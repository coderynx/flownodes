using Flownodes.Sdk.Entities;

namespace Flownodes.Shared.Eventing;

public interface IEventBookGrain : IGrainWithStringKey
{
    ValueTask<EventRegistration> RegisterEventAsync(EventKind kind, FlownodesId targetEntityId);
    ValueTask<HashSet<EventRegistration>> GetEvents();
}