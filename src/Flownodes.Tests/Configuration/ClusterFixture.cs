using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Flownodes.Core.Alerting;
using Flownodes.Core.Resources;
using Flownodes.Worker;
using Flownodes.Worker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Orleans.Hosting;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.Configuration;

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
            var dataObjectBehaviorTest = Substitute.For<IDataCollectorBehaviour>();
            builder.RegisterInstance(dataObjectBehaviorTest)
                .As<IDataCollectorBehaviour>()
                .Keyed<IDataCollectorBehaviour>("TestDataObjectBehavior");

            var deviceBehaviorTest = Substitute.For<IDeviceBehaviour>();
            builder.RegisterInstance(deviceBehaviorTest)
                .As<IDeviceBehaviour>()
                .Keyed<IDeviceBehaviour>("TestDeviceBehavior");

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
                services.AddSingleton<IBehaviourProvider, BehaviourProvider>();
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