using Flownodes.Shared.Models;
using Flownodes.Worker.Interfaces;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

[GrainType("dummy_resource")]
public sealed class DummyResourceGrain : ResourceGrain, IDummyResourceGrain
{
    public DummyResourceGrain(ILogger<DummyResourceGrain> logger, IEnvironmentService environmentService,
        IBehaviourProvider behaviourProvider,
        [PersistentState("dummyResourceConfigurationStore")]
        IPersistentState<ResourceConfigurationStore> configurationStore,
        [PersistentState("dummyResourceMetadataStore")]
        IPersistentState<ResourceMetadataStore> metadataStore,
        [PersistentState("dummyResourceStateStore")]
        IPersistentState<ResourceStateStore> stateStore) :
        base(logger, environmentService, behaviourProvider, configurationStore, metadataStore, stateStore)
    {
    }
}