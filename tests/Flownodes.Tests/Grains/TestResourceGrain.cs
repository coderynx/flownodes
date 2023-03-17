using System.Threading.Tasks;
using Autofac;
using Flownodes.Tests.Interfaces;
using Flownodes.Worker.Resourcing;
using Flownodes.Worker.Services;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Flownodes.Tests.Grains;

[GrainType("other")]
internal sealed class TestResourceGrain : ResourceGrain, ITestResourceGrain
{
    private readonly IContainer _container;

    public TestResourceGrain(ILogger<TestResourceGrain> logger, IEnvironmentService environmentService,
        IPluginProvider pluginProvider, IContainer container) :
        base(logger, environmentService, pluginProvider)
    {
        _container = container;
    }

    public ValueTask<TService> GetService<TService>() where TService : notnull
    {
        var service = _container.Resolve<TService>();
        return ValueTask.FromResult(service);
    }
}