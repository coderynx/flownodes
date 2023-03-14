using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Worker.Modules;
using Flownodes.Worker.Services;
using Mapster;
using MapsterMapper;
using Orleans.Configuration;
using Serilog;
using StackExchange.Redis;

namespace Flownodes.Worker;

// TODO: Implement better separation.
public static partial class Program
{
    private static string? _redisConnectionString;
    private static string? _mongoConnectionString;

    private static void ConfigurePluginsContainer(this IServiceCollection services)
    {
        var containerBuilder = new ContainerBuilder();

        var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (appPath is null) throw new NullReferenceException("Application path should not be null");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(appPath)
            .AddJsonFile("pluginsConfiguration.json", true)
            .Build();

        var pluginServices = new ServiceCollection();
        pluginServices.AddLogging(loggingBuilder =>
        {
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            loggingBuilder.AddSerilog(logger);
        });
        pluginServices.AddHttpClient();

        containerBuilder.Populate(pluginServices);
        containerBuilder.RegisterModule<ComponentsModule>();
        containerBuilder.RegisterInstance<IConfiguration>(configuration.GetSection("Plugins"));

        var container = containerBuilder.Build();
        services.AddSingleton(container);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions();
        var config = new TypeAdapterConfig();
        services.AddSingleton(config);

        services.AddSingleton<IPluginProvider, PluginProvider>();
        services.AddScoped<IMapper, ServiceMapper>();
        services.AddSingleton<IEnvironmentService, EnvironmentService>();
        services.AddHostedService<TestWorker>();
        services.ConfigurePluginsContainer();
    }

    private static void ConfigureProductionStorage(this ISiloBuilder builder)
    {
        builder
            .UseRedisClustering(options =>
            {
                options.ConnectionString = _redisConnectionString;
                options.Database = 0;
            })
            .UseMongoDBClient(_mongoConnectionString)
            .AddMongoDBGrainStorageAsDefault(options =>
            {
                options.DatabaseName = "flownodes-storage";
            });
    }

    private static void UseManualConfiguration(this ISiloBuilder builder)
    {
        var siloPort = Environment.GetEnvironmentVariable("ORLEANS_SILO_PORT") ?? "11111";
        var gatewayPort = Environment.GetEnvironmentVariable("ORLEANS_GATEWAY_PORT") ?? "30000";
        builder.ConfigureEndpoints(int.Parse(siloPort), int.Parse(gatewayPort));
    }

    private static void ConfigureOrleans(HostBuilderContext context, ISiloBuilder builder)
    {
        _redisConnectionString = Environment.GetEnvironmentVariable("REDIS")
                                 ?? context.Configuration.GetConnectionString("redis")
                                 ?? "localhost:6379";
        _mongoConnectionString = Environment.GetEnvironmentVariable("MONGO")
                                 ?? context.Configuration.GetConnectionString("mongo")
                                 ?? "mongodb://locahost:27017";

        builder.ConfigureServices(ConfigureServices);

        var clusterId = Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID") ?? "dev";
        var serviceId = Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID") ?? "flownodes";
        
        builder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = clusterId;
            options.ServiceId = serviceId;
        });

        builder.AddLogStorageBasedLogConsistencyProviderAsDefault();

        // Initialize node with localhost clustering and in-memory persistence.
        if (context.HostingEnvironment.IsDevelopment())
        {
            builder
                .AddMemoryGrainStorageAsDefault()
                .UseLocalhostClustering();
            return;
        }

        // Initialize node with redis clustering and redis persistence.
        builder
            .ConfigureProductionStorage();

        // Check if the node is deployed on a Kubernetes cluster.
        if (Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") is not null)
            builder.UseKubernetesHosting();
        else
            builder.UseManualConfiguration();
    }

    private static void ConfigureLogging(HostBuilderContext context, IServiceProvider provider,
        LoggerConfiguration configuration)
    {
        configuration.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext();
    }
}