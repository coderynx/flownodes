using Flownodes.Edge.Core;
using Flownodes.Edge.Core.Alerting;
using Flownodes.Edge.Core.Resources;
using Orleans;

namespace Flownodes.Edge.ApiGateway.Services;

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