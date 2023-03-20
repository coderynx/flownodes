using System.Net;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
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
            .AddMongoDBGrainStorageAsDefault(options => { options.DatabaseName = "flownodes-storage"; });
    }

    private static void UseManualConfiguration(this ISiloBuilder builder)
    {
        builder.ConfigureEndpoints(
            EnvironmentVariables.OrleansSiloPort ?? 11111,
            EnvironmentVariables.OrleansGatewayPort ?? 30000
        );
    }

    private static void ConfigureOrleans(HostBuilderContext context, ISiloBuilder builder)
    {
        _redisConnectionString = EnvironmentVariables.RedisConnectionString
                                 ?? context.Configuration.GetConnectionString("redis")
                                 ?? "localhost:6379";
        _mongoConnectionString = EnvironmentVariables.MongoConnectionString
                                 ?? context.Configuration.GetConnectionString("mongo")
                                 ?? "mongodb://locahost:27017";

        builder.ConfigureServices(ConfigureServices);


        builder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = EnvironmentVariables.OrleansClusterId ?? "dev";
            options.ServiceId = EnvironmentVariables.OrleansServiceId ?? "flownodes";
        });

        builder.AddLogStorageBasedLogConsistencyProviderAsDefault();
        
        if (context.HostingEnvironment.IsDevelopment())
        {
            var instanceId = context.Configuration.GetValue<int>("InstanceId");
            builder
                .AddMemoryGrainStorageAsDefault()
                .UseLocalhostClustering(
                    11111 + instanceId,
                    30000 + instanceId,
                    new IPEndPoint(IPAddress.Loopback, 11111)
                );

            return;
        }
        
        builder.ConfigureProductionStorage();
        
        if (EnvironmentVariables.KubernetesServiceHost is not null) builder.UseKubernetesHosting();
        else builder.UseManualConfiguration();
    }

    private static void ConfigureLogging(HostBuilderContext context, IServiceProvider provider,
        LoggerConfiguration configuration)
    {
        configuration.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext();
    }
}