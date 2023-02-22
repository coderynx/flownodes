using Flownodes.Shared.Models;
using Flownodes.Tests.Interfaces;
using Flownodes.Worker.Implementations;
using Flownodes.Worker.Services;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Flownodes.Tests.Grains;

[GrainType("dummy_resource")]
public sealed class DummyResourceGrain : ResourceGrain, IDummyResourceGrain
{
    public DummyResourceGrain(ILogger<DummyResourceGrain> logger, IEnvironmentService environmentService,
        IPluginProvider pluginProvider,
        [PersistentState("dummyResourceConfigurationStore")]
        IPersistentState<ResourceConfigurationStore> configurationStore,
        [PersistentState("dummyResourceMetadataStore")]
        IPersistentState<ResourceMetadataStore> metadataStore,
        [PersistentState("dummyResourceStateStore")]
        IPersistentState<ResourceStateStore> stateStore) :
        base(logger, environmentService, pluginProvider, configurationStore, metadataStore, stateStore)
    {
    }
}