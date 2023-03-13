using System.Threading.Tasks;
using Flownodes.Tests.Extensions;
using Flownodes.Worker.Services;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.TestingHost;
using Testcontainers.Redis;
using Xunit;

namespace Flownodes.Tests.Fixtures;

internal static class TestGlobals
{
    public static string? RedisConnectionString { get; set; }
}

public class ClusterFixture : IAsyncLifetime
{
    // TODO: Update ContainerBuilder.
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:latest")
        .WithCleanUp(true)
        .Build();

    public TestCluster? Cluster { get; set; }

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
                services.AddSingleton<IPluginProvider, PluginProvider>();
                services.AddSingleton<IEnvironmentService, EnvironmentService>();

                var config = new TypeAdapterConfig();
                services.AddSingleton(config);
                services.AddScoped<IMapper, ServiceMapper>();
                services.ConfigurePluginsContainer();
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