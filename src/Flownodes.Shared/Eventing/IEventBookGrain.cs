using Flownodes.Sdk.Entities;
using Flownodes.Shared.Entities;

namespace Flownodes.Shared.Eventing;

public interface IEventBookGrain : IEntityGrain
{
    ValueTask<EventRegistration> RegisterEventAsync(EventKind kind, EntityId targetEntityId);
    ValueTask<HashSet<EventRegistration>> GetEvents();
}