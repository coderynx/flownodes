using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Flownodes.Cluster.Core.Alerting;
using Flownodes.Cluster.Core.Resources;
using Flownodes.Cluster.Node.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Orleans.Hosting;
using Orleans.TestingHost;
using WorkflowCore.Interface;
using Xunit;

namespace Flownodes.Cluster.Node.Tests.Configuration;

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
        var workflowHost = Cluster.ServiceProvider.GetService<IWorkflowHost>();
        workflowHost?.Stop();

        await Cluster.StopAllSilosAsync();
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
                services.AddWorkflow(options =>
                {
                    options.UseRedisPersistence(TestGlobals.RedisConnectionString, "flownodes");
                    options.UseRedisLocking(TestGlobals.RedisConnectionString, "flownodes");
                    options.UseRedisQueues(TestGlobals.RedisConnectionString, "flownodes");
                    options.UseRedisEventHub(TestGlobals.RedisConnectionString, "flownodes");
                });
                services.AddWorkflowDSL();
                services.AddTransient<LoggerStep>();
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
                    options.UseJson = true;
                    options.DatabaseNumber = 0;
                }))
                .AddRedisGrainStorageAsDefault(optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.ConnectionString = TestGlobals.RedisConnectionString;
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