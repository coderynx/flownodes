using Flownodes.Cluster.Core;
using Flownodes.Cluster.Core.Alerting;
using Flownodes.Cluster.Core.Resources;
using Orleans;

namespace Flownodes.Cluster.ApiGateway.Services;

public class EdgeService : IEdgeService
{
    private readonly IGrainFactory _grainFactory;

    public EdgeService(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public IResourceManagerGrain GetResourceManager()
    {
        return _grainFactory.GetGrain<IResourceManagerGrain>(Globals.ResourceManagerGrainId);
    }

    public IAlerterGrain GetAlerter()
    {
        return _grainFactory.GetGrain<IAlerterGrain>(Globals.AlerterGrainId);
    }
}