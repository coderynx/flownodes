using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing.Behaviours;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Flownodes.Tests.Extensions;

internal static class TestServiceExtensions
{
    public static void ConfigurePluginsContainer(this IServiceCollection services)
    {
        var containerBuilder = new ContainerBuilder();

        var pluginServices = new ServiceCollection();
        containerBuilder.Populate(pluginServices);

        ConfigureContainer(containerBuilder);

        var container = containerBuilder.Build();
        services.AddSingleton(container);
    }

    private static void ConfigureContainer(this ContainerBuilder builder)
    {
        var deviceBehaviorTest = Substitute.For<IBehaviour>();
        builder.RegisterInstance(deviceBehaviorTest)
            .As<IBehaviour>()
            .Keyed<IBehaviour>("TestDeviceBehavior");

        var alertBehaviorTest = Substitute.For<IAlerterDriver>();
        builder.RegisterInstance(alertBehaviorTest)
            .As<IAlerterDriver>()
            .Keyed<IAlerterDriver>("TestAlerterDriver");
    }
}