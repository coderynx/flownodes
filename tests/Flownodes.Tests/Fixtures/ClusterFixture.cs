using System.Threading.Tasks;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.TestingHost;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Flownodes.Tests.Fixtures;

internal static class TestGlobals
{
    public static string? RedisConnectionString { get; set; }
}

public class ClusterFixture : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:latest")
        .WithCleanUp(true)
        .Build();

    public TestCluster? Cluster { get; private set; }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        TestGlobals.RedisConnectionString = _redisContainer.GetConnectionString();

        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();

        Cluster = builder.Build();
        await Cluster.DeployAsync();
    }

    public async Task DisposeAsync()
    {
        await Cluster!.KillSiloAsync(Cluster.Primary);
        await _redisContainer.StopAsync();
    }

    private class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IComponentProvider, TestComponentProvider>();
                services.AddSingleton<IEnvironmentService, EnvironmentService>();
            });

            siloBuilder
                .AddLogStorageBasedLogConsistencyProviderAsDefault()
                .UseRedisClustering(options =>
                {
                    options.ConfigurationOptions = ConfigurationOptions.Parse(TestGlobals.RedisConnectionString!);
                })
                .AddRedisGrainStorageAsDefault(options =>
                {
                    options.ConfigurationOptions = ConfigurationOptions.Parse(TestGlobals.RedisConnectionString!);
                });
        }
    }
}

[CollectionDefinition("TestCluster")]
public class ClusterCollection : ICollectionFixture<ClusterFixture>
{
}