using System.Threading.Tasks;
using Autofac;
using Flownodes.Tests.Interfaces;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Resourcing;
using Flownodes.Worker.Services;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Flownodes.Tests.Grains;

internal sealed class TestResourceGrain : ResourceGrain, ITestResourceGrain
{
    public TestResourceGrain(ILogger<TestResourceGrain> logger, IEnvironmentService environmentService,
        IComponentProvider componentProvider) : base(logger, environmentService, componentProvider)
    {
    }
}