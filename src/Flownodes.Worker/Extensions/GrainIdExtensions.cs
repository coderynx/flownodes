using Flownodes.Sdk.Entities;
using Orleans.Runtime;

namespace Flownodes.Worker.Extensions;

public static class GrainIdExtensions
{
    public static EntityId ToFlownodesId(this GrainId id)
    {
        return (EntityId)id.Key.ToString()!;
    }
}