using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Flownodes.Edge.Core.Alerting;
using Flownodes.Edge.Core.Resources;
using Flownodes.Edge.Node;
using Flownodes.Edge.Node.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Orleans.Hosting;
using Orleans.TestingHost;
using Xunit;
using EnvironmentOptions = Flownodes.Edge.Node.EnvironmentOptions;

namespace Flownodes.Edge.Tests.Configuration;

internal static class TestGlobals
{
    public static string? RedisConnectionString { get; set; }
}

public class ClusterFixture : IAsyncLifetime
{
    private readonly RedisTestcontainer _redisContainer = new TestcontainersBuilder<RedisTestcontainer>()
        .WithImage("redis:latest")
        .WithDatabase(new RedisTestcontainerConfiguration())
        .WithCleanUp(true)
        .Build();

    public TestCluster Cluster { get; set; }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        TestGlobals.RedisConnectionString = _redisContainer.ConnectionString;

        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        builder.AddSiloBuilderConfigurator<HostConfigurator>();

        Cluster = builder.Build();
        await Cluster.DeployAsync();
    }

    public async Task DisposeAsync()
    {
        await Cluster.KillSiloAsync(Cluster.Primary);
        await _redisContainer.StopAsync();
    }

    private class HostConfigurator : IHostConfigurator
    {
        public void Configure(IHostBuilder hostBuilder)
        {
            hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(ConfigureContainer);
        }

        private static void ConfigureContainer(ContainerBuilder builder)
        {
            var dataObjectBehaviorTest = Substitute.For<IDataCollectorBehavior>();
            builder.RegisterInstance(dataObjectBehaviorTest)
                .As<IDataCollectorBehavior>()
                .Keyed<IDataCollectorBehavior>("TestDataObjectBehavior");

            var deviceBehaviorTest = Substitute.For<IDeviceBehavior>();
            builder.RegisterInstance(deviceBehaviorTest)
                .As<IDeviceBehavior>()
                .Keyed<IDeviceBehavior>("TestDeviceBehavior");

            var alertBehaviorTest = Substitute.For<IAlerterDriver>();
            builder.RegisterInstance(alertBehaviorTest)
                .As<IAlerterDriver>()
                .Keyed<IAlerterDriver>("TestAlerterDriver");
        }
    }

    private class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IBehaviorProvider, BehaviorProvider>();
                services.AddSingleton<IEnvironmentService, EnvironmentService>();
            });

            siloBuilder
                .UseRedisClustering(options =>
                {
                    options.ConnectionString = TestGlobals.RedisConnectionString;
                    options.Database = 1;
                })
                .AddRedisGrainStorage("flownodes", optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.ConnectionString = TestGlobals.RedisConnectionString;
                    options.DatabaseNumber = 0;
                })).AddRedisGrainStorageAsDefault(optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.ConnectionString = TestGlobals.RedisConnectionString;
                    options.DatabaseNumber = 0;
                }));
        }
    }
}

[CollectionDefinition(nameof(ClusterFixture))]
public class ClusterCollection : ICollectionFixture<ClusterFixture>
{
}