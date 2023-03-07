using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing;
using Flownodes.Worker.Services;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Orleans.Hosting;
using Orleans.TestingHost;
using Xunit;
using ContainerBuilder = Autofac.ContainerBuilder;

namespace Flownodes.Tests.Configuration;

internal static class TestGlobals
{
    public static string? RedisConnectionString { get; set; }
}

public class ClusterFixture : IAsyncLifetime
{
    private readonly RedisTestcontainer _redisContainer = new ContainerBuilder<RedisTestcontainer>()
        .WithImage("redis:latest")
        .WithDatabase(new RedisTestcontainerConfiguration())
        .WithCleanUp(true)
        .Build();

    public TestCluster? Cluster { get; set; }

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
        await Cluster!.KillSiloAsync(Cluster.Primary);
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

    private class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IPluginProvider, PluginProvider>();
                services.AddSingleton<IEnvironmentService, EnvironmentService>();

                var config = new TypeAdapterConfig();
                services.AddSingleton(config);
                services.AddScoped<IMapper, ServiceMapper>();
            });

            siloBuilder
                .UseRedisClustering(options =>
                {
                    options.ConnectionString = TestGlobals.RedisConnectionString;
                    options.Database = 1;
                }).AddRedisGrainStorageAsDefault(optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.ConnectionString = TestGlobals.RedisConnectionString;
                    options.DatabaseNumber = 0;
                    options.DeleteOnClear = true;
                }));
        }
    }
}

[CollectionDefinition("TestCluster")]
public class ClusterCollection : ICollectionFixture<ClusterFixture>
{
}