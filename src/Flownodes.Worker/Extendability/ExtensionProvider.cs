using System.Reflection;
using Autofac;
using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing.Attributes;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Worker.Extendability.Modules;

namespace Flownodes.Worker.Extendability;

public class ExtensionProvider : IExtensionProvider
{
    private readonly ILogger<ExtensionProvider> _logger;
    private IContainer? _container;

    public ExtensionProvider(ILogger<ExtensionProvider> logger)
    {
        _logger = logger;
    }

    public IBehaviour? GetBehaviour(string id)
    {
        if (_container is null) throw new InvalidOperationException("The container is not built yet");

        _logger.LogDebug("Retrieving behaviour {@DeviceBehaviourId}", id);
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

        containerBuilder.RegisterModule<ExtensionsModule>();
        containerBuilder.RegisterModule<ExtensionsContainerModule>();

        _container = containerBuilder.Build();

        var behaviours = _container.ResolveOptional<IEnumerable<IBehaviour>>();
        if (behaviours is not null)
        {
            foreach (var behaviour in behaviours)
            {
                var behaviourId = behaviour.GetType().GetCustomAttribute<BehaviourIdAttribute>();
                _logger.LogInformation("Registered behaviour {@BehaviourId}", behaviourId?.Id);
            }
        }

        _logger.LogInformation("Built extensions container");
    }
}