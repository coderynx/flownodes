using Flownodes.Sdk.Entities;
using Flownodes.Shared.Entities;

namespace Flownodes.Shared.Eventing;

public interface IEventBookGrain : IEntityGrain
{
    ValueTask<EventRegistration> RegisterEventAsync(EventKind kind, FlownodesId targetEntityId);
    ValueTask<HashSet<EventRegistration>> GetEvents();
}