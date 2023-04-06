using Autofac;
using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing;
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

    public TBehaviour? ResolveBehaviour<TBehaviour, TContext>(string id, TContext context) where TBehaviour : IBehaviour
        where TContext : ResourceContext
    {
        if (_container is null) throw new InvalidOperationException("The container is not built yet");

        _logger.LogDebug("Retrieving behaviour {@DeviceBehaviourId}", id);

        var contextParameter = new TypedParameter(typeof(TContext), context);
        return (TBehaviour?)_container.ResolveOptionalKeyed<IBehaviour>(id, contextParameter);
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

        _logger.LogInformation("Built extensions container");
    }
}