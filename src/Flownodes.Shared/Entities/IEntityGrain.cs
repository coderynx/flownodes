using Flownodes.Sdk.Entities;

namespace Flownodes.Shared.Entities;

public interface IEntityGrain : IGrainWithStringKey
{
    ValueTask<EntityId> GetId();
}