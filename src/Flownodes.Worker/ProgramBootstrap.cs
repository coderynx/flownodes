using System.Reflection;
using Autofac;
using Flownodes.Worker.Modules;
using Flownodes.Worker.Services;
using Mapster;
using MapsterMapper;
using Orleans.Configuration;
using Serilog;

namespace Flownodes.Worker;

// TODO: Implement better separation.
public static partial class Program
{
    private static string? _redisConnectionString;

    private static void ConfigureContainer(ContainerBuilder builder)
    {
        builder.RegisterModule<ComponentsModule>();
    }

    private static void ConfigureAppConfiguration(IConfigurationBuilder builder)
    {
        var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (appPath is null) throw new NullReferenceException("Application path should not be null");

        builder.SetBasePath(appPath)
            .AddJsonFile("configuration.json", true);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions();
        services.AddSingleton<IPluginProvider, PluginProvider>();

        services.AddHttpClient();

        // Setting up environment.
        var alertManagerName = Environment.GetEnvironmentVariable("ALERT_MANAGER_NAME") ?? "alert_manager";
        var resourceManagerName = Environment.GetEnvironmentVariable("RESOURCE_MANAGER_NAME") ?? "resource_manager";
        var tenantManagerName = Environment.GetEnvironmentVariable("TENANT_MANAGER_NAME") ?? "tenant_manager";

        services.Configure<EnvironmentOptions>(options =>
        {
            options.AlertManagerName = alertManagerName;
            options.ResourceManagerName = resourceManagerName;
            options.TenantManagerName = tenantManagerName;
        });

        var config = new TypeAdapterConfig();
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddSingleton<IEnvironmentService, EnvironmentService>();
        services.AddHostedService<TestWorker>();
    }

    private static void ConfigureProductionStorage(this ISiloBuilder builder)
    {
        builder
            .UseRedisClustering(options =>
            {
                options.ConnectionString = _redisConnectionString;
                options.Database = 0;
            })
            .AddRedisGrainStorageAsDefault(options =>
            {
                options.ConnectionString = _redisConnectionString;
                options.DatabaseNumber = 1;
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
        _redisConnectionString = $"{Environment.GetEnvironmentVariable("REDIS")}:6379";

        var configurationConnectionString = context.Configuration.GetConnectionString("redis");
        if (configurationConnectionString is not null) _redisConnectionString = configurationConnectionString;

        builder.ConfigureServices(ConfigureServices);

        var clusterId = Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID") ?? "dev";
        var serviceId = Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID") ?? "flownodes";
        builder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = clusterId;
            options.ServiceId = serviceId;
        });


        //builder.UseDashboard(_ => { });

        // Initialize node with localhost clustering and in-memory persistence.
        if (context.HostingEnvironment.IsDevelopment())
        {
            builder
                .AddMemoryGrainStorageAsDefault()
                .UseLocalhostClustering();
            return;
        }

        // Initialize node with redis clustering and redis persistence.
        builder.ConfigureProductionStorage();

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