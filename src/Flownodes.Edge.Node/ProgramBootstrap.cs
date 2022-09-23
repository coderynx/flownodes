using System.Reflection;
using Autofac;
using Flownodes.Edge.Node.Automation;
using Flownodes.Edge.Node.Modules;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;

namespace Flownodes.Edge.Node;

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
        builder.SetBasePath(appPath)
            .AddJsonFile("configuration.json", true);
    }

    private static ISiloBuilder ConfigureDevelopmentWorkflow(this ISiloBuilder builder)
    {
        return builder.ConfigureServices(services =>
        {
            services.AddWorkflow();
            services.AddWorkflowDSL();
            services.AddTransient<LoggerStep>();
        });
    }

    private static void ConfigureProductionWorkflow(this ISiloBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddWorkflow(options =>
            {
                options.UseRedisPersistence(_redisConnectionString, "flownodes");
                options.UseRedisLocking(_redisConnectionString, "flownodes");
                options.UseRedisQueues(_redisConnectionString, "flownodes");
                options.UseRedisEventHub(_redisConnectionString, "flownodes");
            });

            services.AddWorkflowDSL();
            services.AddTransient<LoggerStep>();
        });
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions();
        services.AddSingleton<IBehaviorProvider, BehaviorProvider>();
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
            .AddRedisGrainStorage("flownodes", options =>
            {
                options.ConnectionString = _redisConnectionString;
                options.DatabaseNumber = 1;
                options.UseJson = true;
            })
            .AddRedisGrainStorageAsDefault(options =>
            {
                options.ConnectionString = _redisConnectionString;
                options.DatabaseNumber = 2;
                options.UseJson = true;
            });
    }

    private static void UseManualConfiguration(this ISiloBuilder builder)
    {
        var siloPort = Environment.GetEnvironmentVariable("SILO_PORT") ?? "11111";
        var gatewayPort = Environment.GetEnvironmentVariable("GATEWAY_PORT") ?? "30000";
        builder.ConfigureEndpoints(int.Parse(siloPort), int.Parse(gatewayPort));

        builder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = Environment.GetEnvironmentVariable("CLUSTER-ID");
            options.ServiceId = Environment.GetEnvironmentVariable("SERVICE-ID");
        });
    }

    private static void ConfigureOrleans(HostBuilderContext context, ISiloBuilder builder)
    {
        _redisConnectionString = $"{Environment.GetEnvironmentVariable("REDIS")}:6379";

        var configurationConnectionString = context.Configuration.GetConnectionString("redis");
        if (configurationConnectionString is not null) _redisConnectionString = configurationConnectionString;

        builder.ConfigureServices(ConfigureServices);

        // Initialize node with localhost clustering and in-memory persistence.
        if (context.HostingEnvironment.IsDevelopment())
        {
            builder.ConfigureDevelopmentWorkflow();
            builder
                .AddMemoryGrainStorage("flownodes")
                .UseLocalhostClustering()
                .UseDashboard();
            return;
        }

        // Initialize node with redis clustering and redis persistence.
        builder.ConfigureProductionWorkflow();
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
        configuration.ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext();
    }
}