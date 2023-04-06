
using Autofac;
using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Shared.Resourcing.Exceptions;
using Flownodes.Worker.Extendability;
using NSubstitute;

namespace Flownodes.Tests;

public class TestExtensionProvider : IExtensionProvider
{
    private IContainer _container = null!;

    public TestExtensionProvider()
    {
        BuildContainer();
    }

    public IBehaviour? ResolveBehaviour(string id, ResourceContext context)
    {
        return _container.ResolveOptionalKeyed<IBehaviour>(id);
    }

    public TBehaviour ResolveBehaviour<TBehaviour, TContext>(string id, TContext context) where TBehaviour : IBehaviour
        where TContext : ResourceContext
    {
        return (TBehaviour?)_container.ResolveOptionalKeyed<IBehaviour>(id)
               ?? throw new ResourceBehaviourNotRegisteredException(id);
    }

    public IAlerterDriver? GetAlerterDriver(string id)
    {
        return _container.ResolveOptionalKeyed<IAlerterDriver>(id);
    }

    public void BuildContainer()
    {
        var containerBuilder = new ContainerBuilder();

        var deviceBehaviorTest = Substitute.For<IReadableDeviceBehaviour, IWritableDeviceBehaviour>();
        containerBuilder.RegisterInstance(deviceBehaviorTest)
            .As<IBehaviour>()
            .Keyed<IBehaviour>("TestDeviceBehaviour");

        var alertBehaviorTest = Substitute.For<IAlerterDriver>();
        containerBuilder.RegisterInstance(alertBehaviorTest)
            .As<IAlerterDriver>()
            .Keyed<IAlerterDriver>("TestAlerterDriver");

        _container = containerBuilder.Build();
    }
}