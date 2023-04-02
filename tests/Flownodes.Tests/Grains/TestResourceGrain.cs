using Flownodes.Tests.Interfaces;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Resourcing;
using Flownodes.Worker.Services;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Flownodes.Tests.Grains;

internal sealed class TestResourceGrain : ResourceGrain, ITestResourceGrain
{
    public TestResourceGrain(ILogger<TestResourceGrain> logger, IEnvironmentService environmentService,
        IExtensionProvider extensionProvider, IPersistentStateFactory stateFactory, IGrainContext grainContext)
        : base(logger, environmentService, extensionProvider, stateFactory, grainContext)
    {
    }
}