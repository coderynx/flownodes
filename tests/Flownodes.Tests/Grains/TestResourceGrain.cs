using System.Threading.Tasks;
using Autofac;
using Flownodes.Tests.Interfaces;
using Flownodes.Worker.Implementations;
using Flownodes.Worker.Services;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Flownodes.Tests.Grains;

[GrainType("other")]
internal sealed class TestResourceGrain : ResourceGrain, ITestResourceGrain
{
    private readonly IContainer _container;

    public TestResourceGrain(ILogger<TestResourceGrain> logger, IEnvironmentService environmentService,
        IPluginProvider pluginProvider, IPersistentStateFactory persistentStateFactory, IGrainContext grainContext,
        IContainer container) :
        base(logger, environmentService, pluginProvider, persistentStateFactory, grainContext)
    {
        _container = container;
    }

    public ValueTask<TService> GetService<TService>() where TService : notnull
    {
        var service = _container.Resolve<TService>();
        return ValueTask.FromResult(service);
    }
}