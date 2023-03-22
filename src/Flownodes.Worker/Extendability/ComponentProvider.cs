using Autofac;
using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Worker.Extendability.Modules;

namespace Flownodes.Worker.Extendability;

public class ComponentProvider : IComponentProvider
{
    private readonly ILogger<ComponentProvider> _logger;
    private IContainer? _container;

    public ComponentProvider(ILogger<ComponentProvider> logger)
    {
        _logger = logger;
    }

    public IBehaviour? GetBehaviour(string id)
    {
        if (_container is null) throw new InvalidOperationException("The container is not built yet");
        
        _logger.LogDebug("Retrieving behavior {@DeviceBehaviourId}", id);
        return _container.ResolveOptionalKeyed<IBehaviour>(id);
    }

    public IAlerterDriver? GetAlerterDriver(string id)
    {
        if (_container is null) throw new InvalidOperationException("The container is not built yet");
        
        _logger.LogDebug("Retrieving alerter driver {@AlerterDriverId}", id);
        return _container.ResolveOptionalKeyed<IAlerterDriver>(id);
    }

    public void BuildContainer()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.RegisterModule<ComponentsModule>();
        containerBuilder.RegisterModule<ComponentsContainerModule>();

        _container = containerBuilder.Build();
        _logger.LogInformation("Built components container");
    }
}