using Flownodes.Worker.Interfaces;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

[GrainType("dummy_resource")]
public sealed class DummyResourceGrain : ResourceGrain, IDummyResourceGrain
{
    public DummyResourceGrain(ILogger<DummyResourceGrain> logger,
        [PersistentState("dummyResourcePersistence", "flownodes")]
        IPersistentState<ResourcePersistence> persistence,
        IEnvironmentService environmentService, IBehaviourProvider behaviourProvider) :
        base(logger, persistence, environmentService, behaviourProvider)
    {
    }
}