using Flownodes.Sdk.Entities;
using Orleans.Runtime;

namespace Flownodes.Worker.Extensions;

public static class GrainIdExtensions
{
    public static FlownodesId ToFlownodesId(this GrainId id)
    {
        return (FlownodesId)id.Key.ToString()!;
    }
}