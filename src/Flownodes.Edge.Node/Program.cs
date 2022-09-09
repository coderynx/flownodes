using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Edge.Node.Automation;
using Flownodes.Edge.Node.Modules;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;

namespace Flownodes.Edge.Node;

public static class Program
{
    private static void ConfigureContainer(ContainerBuilder builder)
    {
        builder.RegisterModule<ComponentsModule>();
    }

    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(ConfigureContainer)
            .ConfigureAppConfiguration(builder =>
            {
                var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                builder.SetBasePath(appPath)
                    .AddJsonFile("configuration.json", true);
            })
            .UseOrleans((host, builder) =>
            {
                var orleansClustering = host.Configuration.GetSection("OrleansClustering")
                    .Get<OrleansClusteringConfiguration>();
                var orleansEndpoints = host.Configuration.GetSection("OrleansEndpoints")
                    .Get<OrleansEndpointsConfiguration>();
                var redisClustering = host.Configuration.GetSection("RedisClustering")
                    .Get<RedisClusteringConfiguration>();
                var redisPersistence = host.Configuration.GetSection("RedisPersistence")
                    .Get<RedisPersistenceConfiguration>();
                var redisWorkflow = host.Configuration.GetSection("RedisWorkflow")
                    .Get<RedisWorkflowConfiguration>();

                builder
                    .ConfigureServices(services =>
                    {
                        services.AddOptions();
                        services.AddWorkflow(options =>
                        {
                            options.UseRedisPersistence(redisWorkflow.ConnectionString, "flownodes");
                            options.UseRedisLocking(redisWorkflow.ConnectionString, "flownodes");
                            options.UseRedisQueues(redisWorkflow.ConnectionString, "flownodes");
                            options.UseRedisEventHub(redisWorkflow.ConnectionString, "flownodes");
                        });
                        services.AddWorkflowDSL();
                        services.AddTransient<LoggerStep>();
                        services.AddHostedService<Worker>();
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = orleansClustering.ClusterId;
                        options.ServiceId = orleansClustering.ServiceId;
                    })
                    .ConfigureEndpoints(orleansEndpoints.SiloPort, orleansEndpoints.GatewayPort)
                    .UseRedisClustering(options =>
                    {
                        options.ConnectionString = redisClustering.ConnectionString;
                        options.Database = redisClustering.DatabaseId;
                    })
                    .AddRedisGrainStorage("flownodes", options =>
                    {
                        options.ConnectionString = redisPersistence.ConnectionString;
                        options.DatabaseNumber = redisPersistence.DatabaseId;
                        options.UseJson = redisPersistence.UseJson;
                    })
                    .AddRedisGrainStorageAsDefault(options =>
                    {
                        options.ConnectionString = redisPersistence.ConnectionString;
                        options.DatabaseNumber = redisPersistence.DatabaseId;
                        options.UseJson = redisPersistence.UseJson;
                    })
                    .UseDashboard();
            })
            .UseSerilog((hostingContext, _, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.FromLogContext())
            .Build();

        await host.RunAsync();
    }

    private record OrleansClusteringConfiguration
    {
        public string? ClusterId { get; init; }
        public string? ServiceId { get; init; }
    }

    private record OrleansEndpointsConfiguration
    {
        public int SiloPort { get; set; }
        public int GatewayPort { get; set; }
    }

    private record RedisPersistenceConfiguration
    {
        public string? ConnectionString { get; init; }
        public int DatabaseId { get; init; }
        public bool UseJson { get; init; }
    }

    private record RedisClusteringConfiguration
    {
        public string? ConnectionString { get; init; }
        public int DatabaseId { get; init; }
    }

    private record RedisWorkflowConfiguration
    {
        public string? ConnectionString { get; set; }
    }
}