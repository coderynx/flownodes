using Autofac;
using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Worker.Extendability;
using NSubstitute;

namespace Flownodes.Tests;

public class TestComponentProvider : IComponentProvider
{
    private IContainer _container = null!;

    public TestComponentProvider()
    {
        BuildContainer();
    }

    public IBehaviour? GetBehaviour(string id)
    {
        return _container.ResolveOptionalKeyed<IBehaviour>(id);
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