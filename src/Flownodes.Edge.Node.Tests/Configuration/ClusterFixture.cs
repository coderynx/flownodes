using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Flownodes.Edge.Core.Alerting;
using Flownodes.Edge.Core.Resources;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Orleans.Hosting;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Edge.Node.Tests.Configuration;

internal static class TestGlobals
{
    public static string? ConnectionString { get; set; }
}

public class ClusterFixture : IAsyncLifetime
{
    private readonly RedisTestcontainer _testContainer = new TestcontainersBuilder<RedisTestcontainer>()
        .WithImage("redis:latest")
        .WithDatabase(new RedisTestcontainerConfiguration())
        .WithCleanUp(true)
        .Build();

    public TestCluster Cluster { get; set; }

    public async Task InitializeAsync()
    {
        await _testContainer.StartAsync();
        TestGlobals.ConnectionString = _testContainer.ConnectionString;

        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        builder.AddSiloBuilderConfigurator<HostConfigurator>();
        Cluster = builder.Build();
        await Cluster.DeployAsync();
    }

    public async Task DisposeAsync()
    {
        await Cluster.StopAllSilosAsync();
        await _testContainer.StopAsync();
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
            siloBuilder
                .UseRedisClustering(options =>
                {
                    options.ConnectionString = TestGlobals.ConnectionString;
                    options.Database = 1;
                })
                .AddRedisGrainStorage("flownodes", optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.ConnectionString = TestGlobals.ConnectionString;
                    options.UseJson = true;
                    options.DatabaseNumber = 0;
                }))
                .AddRedisGrainStorageAsDefault(optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.ConnectionString = TestGlobals.ConnectionString;
                    options.UseJson = true;
                    options.DatabaseNumber = 0;
                }));
        }
    }
}

[CollectionDefinition(nameof(ClusterFixture))]
public class ClusterCollection : ICollectionFixture<ClusterFixture>
{
}