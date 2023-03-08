using Flownodes.Tests.Interfaces;
using Flownodes.Worker.Implementations;
using Flownodes.Worker.Services;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Flownodes.Tests.Grains;

[GrainType("dummy_resource")]
internal sealed class DummyResourceGrain : ResourceGrain, IDummyResourceGrain
{
    public DummyResourceGrain(ILogger<DummyResourceGrain> logger, IEnvironmentService environmentService,
        IPluginProvider pluginProvider, IPersistentStateFactory persistentStateFactory, IGrainContext grainContext) :
        base(logger, environmentService, pluginProvider, persistentStateFactory, grainContext)
    {
    }
}