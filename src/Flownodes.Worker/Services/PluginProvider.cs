using Autofac;
using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing.Behaviours;

namespace Flownodes.Worker.Services;

public class PluginProvider : IPluginProvider
{
    private readonly IContainer _container;
    private readonly ILogger<PluginProvider> _logger;

    public PluginProvider(ILogger<PluginProvider> logger, IContainer container)
    {
        _logger = logger;
        _container = container;
    }

    public IBehaviour? GetBehaviour(string id)
    {
        _logger.LogDebug("Retrieving behavior {@DeviceBehaviourId}", id);
        return _container.ResolveOptionalKeyed<IBehaviour>(id);
    }

    public IAlerterDriver? GetAlerterDriver(string id)
    {
        _logger.LogDebug("Retrieving alerter driver {@AlerterDriverId}", id);
        return _container.ResolveOptionalKeyed<IAlerterDriver>(id);
    }
}