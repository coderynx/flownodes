using System.Threading.Tasks;
using Flownodes.Tests.Extensions;
using Flownodes.Worker.Services;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.TestingHost;
using Testcontainers.MongoDb;
using Testcontainers.Redis;
using Xunit;

namespace Flownodes.Tests.Fixtures;

internal static class TestGlobals
{
    public static string? RedisConnectionString { get; set; }
    public static string? MongoConnectionString { get; set; }
}

public class ClusterFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:latest")
        .WithCleanUp(true)
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:latest")
        .WithCleanUp(true)
        .Build();

    public TestCluster? Cluster { get; set; }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        TestGlobals.RedisConnectionString = _redisContainer.GetConnectionString();

        await _mongoContainer.StartAsync();
        TestGlobals.MongoConnectionString = _mongoContainer.GetConnectionString();

        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();

        Cluster = builder.Build();
        await Cluster.DeployAsync();
    }

    public async Task DisposeAsync()
    {
        await Cluster!.KillSiloAsync(Cluster.Primary);
        await _redisContainer.StopAsync();
        await _mongoContainer.StopAsync();
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
                .AddLogStorageBasedLogConsistencyProviderAsDefault()
                .UseRedisClustering(options =>
                {
                    options.ConnectionString = TestGlobals.RedisConnectionString;
                    options.Database = 0;
                })
                .UseMongoDBClient(TestGlobals.MongoConnectionString)
                .AddMongoDBGrainStorageAsDefault(options => { options.DatabaseName = "flownodes-storage"; });
        }
    }
}

[CollectionDefinition("TestCluster")]
public class ClusterCollection : ICollectionFixture<ClusterFixture>
{
}