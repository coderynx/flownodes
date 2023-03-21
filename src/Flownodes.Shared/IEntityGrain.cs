using Flownodes.Sdk;

namespace Flownodes.Shared;

public interface IEntityGrain : IGrainWithStringKey
{
    ValueTask<FlownodesId> GetId();
}