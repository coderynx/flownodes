using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Sdk.Resourcing;

namespace Flownodes.Worker.Services;

public class BehaviourProvider : IBehaviourProvider
{
    private readonly ILifetimeScope _lifetimeScope;
    private readonly ILogger<BehaviourProvider> _logger;

    public BehaviourProvider(IServiceProvider serviceProvider, ILogger<BehaviourProvider> logger)
    {
        _logger = logger;
        _lifetimeScope = serviceProvider.GetAutofacRoot();
    }

    public IBehaviour? GetBehaviour(string id)
    {
        _logger.LogDebug("Retrieving behavior {DeviceBehaviourId}", id);
        return _lifetimeScope.ResolveOptionalKeyed<IBehaviour>(id);
    }
}