using Flownodes.Tests.Interfaces;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Resourcing;
using Flownodes.Worker.Services;
using Microsoft.Extensions.Logging;

namespace Flownodes.Tests.Grains;

internal sealed class TestResourceGrain : ResourceGrain, ITestResourceGrain
{
    public TestResourceGrain(ILogger<TestResourceGrain> logger, IEnvironmentService environmentService,
        IExtensionProvider extensionProvider) : base(logger, environmentService, extensionProvider)
    {
    }
}