using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Flownodes.Edge.Core.Alerting;
using Flownodes.Edge.Core.Resources;
using Flownodes.Edge.Node.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Orleans.Hosting;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Edge.Node.Tests.Configuration;

internal static class TestGlobals
{
    public static string? OrleansConnectionString { get; set; }
    public static string? WorkflowConnectionString { get; set; }
}

public class ClusterFixture : IAsyncLifetime
{
    private readonly RedisTestcontainer _orleansRedis = new TestcontainersBuilder<RedisTestcontainer>()
        .WithImage("redis:latest")
        .WithDatabase(new RedisTestcontainerConfiguration())
        .WithCleanUp(true)
        .Build();

    private readonly RedisTestcontainer _workflowRedis = new TestcontainersBuilder<RedisTestcontainer>()
        .WithImage("redis:latest")
        .WithDatabase(new RedisTestcontainerConfiguration())
        .WithCleanUp(true)
        .Build();

    public TestCluster Cluster { get; set; }

    public async Task InitializeAsync()
    {
        await _orleansRedis.StartAsync();
        TestGlobals.OrleansConnectionString = _orleansRedis.ConnectionString;

        await _workflowRedis.StartAsync();
        TestGlobals.WorkflowConnectionString = _workflowRedis.ConnectionString;

        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        builder.AddSiloBuilderConfigurator<HostConfigurator>();
        Cluster = builder.Build();
        await Cluster.DeployAsync();
    }

    public async Task DisposeAsync()
    {
        await Cluster.StopAllSilosAsync();
        await _orleansRedis.StopAsync();
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
                services.AddWorkflow(options =>
                {
                    options.UseRedisPersistence(TestGlobals.WorkflowConnectionString, "flownodes");
                    options.UseRedisLocking(TestGlobals.WorkflowConnectionString, "flownodes");
                    options.UseRedisQueues(TestGlobals.WorkflowConnectionString, "flownodes");
                    options.UseRedisEventHub(TestGlobals.WorkflowConnectionString, "flownodes");
                });
                services.AddWorkflowDSL();
                services.AddTransient<LoggerStep>();
            });

            siloBuilder
                .UseRedisClustering(options =>
                {
                    options.ConnectionString = TestGlobals.OrleansConnectionString;
                    options.Database = 1;
                })
                .AddRedisGrainStorage("flownodes", optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.ConnectionString = TestGlobals.OrleansConnectionString;
                    options.UseJson = true;
                    options.DatabaseNumber = 0;
                }))
                .AddRedisGrainStorageAsDefault(optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.ConnectionString = TestGlobals.OrleansConnectionString;
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