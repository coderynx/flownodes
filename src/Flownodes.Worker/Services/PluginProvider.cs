using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing;

namespace Flownodes.Worker.Services;

public class PluginProvider : IPluginProvider
{
    private readonly ILifetimeScope _lifetimeScope;
    private readonly ILogger<PluginProvider> _logger;

    public PluginProvider(IServiceProvider serviceProvider, ILogger<PluginProvider> logger)
    {
        _logger = logger;
        _lifetimeScope = serviceProvider.GetAutofacRoot();
    }

    public IBehaviour? GetBehaviour(string id)
    {
        _logger.LogDebug("Retrieving behavior {@DeviceBehaviourId}", id);
        return _lifetimeScope.ResolveOptionalKeyed<IBehaviour>(id);
    }

    public IAlerterDriver? GetAlerterDriver(string id)
    {
        _logger.LogDebug("Retrieving alerter driver {@AlerterDriverId}", id);
        return _lifetimeScope.ResolveOptionalKeyed<IAlerterDriver>(id);
    }
}