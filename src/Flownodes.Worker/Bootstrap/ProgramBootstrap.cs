using Carter;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Services;
using Orleans.Configuration;
using Serilog;

namespace Flownodes.Worker.Bootstrap;

internal static class Bootstrap
{
    public static void ConfigureWebServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddAuthorization();
        services.AddMediatR(config => { config.RegisterServicesFromAssembly(typeof(GetTenantRequest).Assembly); });
        services.AddCarter();
    }

    private static void ConfigureOrleansServices(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddSingleton<IComponentProvider, ComponentProvider>();
        services.AddSingleton<IEnvironmentService, EnvironmentService>();
        services.AddHostedService<TestWorker>();
    }

    private static void ConfigureDevelopment(this ISiloBuilder builder)
    {
        builder.AddMemoryGrainStorageAsDefault()
            .UseLocalhostClustering()
            .UseDashboard();
    }

    private static void ConfigureProduction(this ISiloBuilder builder, HostBuilderContext context)
    {
        var redisConnectionString = EnvironmentVariables.RedisConnectionString
                                    ?? context.Configuration.GetConnectionString("redis")
                                    ?? "localhost:6379";
        var mongoConnectionString = EnvironmentVariables.MongoConnectionString
                                    ?? context.Configuration.GetConnectionString("mongo")
                                    ?? "mongodb://locahost:27017";

        builder
            .UseRedisClustering(options =>
            {
                options.ConnectionString = redisConnectionString;
                options.Database = 0;
            })
            .UseMongoDBClient(mongoConnectionString)
            .AddMongoDBGrainStorageAsDefault(options => { options.DatabaseName = "flownodes-storage"; });
    }

    public static void ConfigureOrleans(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseOrleans((context, siloBuilder) =>
        {
            siloBuilder.Services.ConfigureOrleansServices();

            siloBuilder.ConfigureEndpoints(
                EnvironmentVariables.OrleansSiloPort ?? 11111,
                EnvironmentVariables.OrleansGatewayPort ?? 30000
            );

            siloBuilder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = EnvironmentVariables.OrleansClusterId ?? "dev";
                options.ServiceId = EnvironmentVariables.OrleansServiceId ?? "flownodes";
            });

            siloBuilder.AddLogStorageBasedLogConsistencyProviderAsDefault();

            if (context.HostingEnvironment.IsDevelopment())
                siloBuilder.ConfigureDevelopment();

            if (context.HostingEnvironment.IsStaging() || context.HostingEnvironment.IsProduction())
                siloBuilder.ConfigureProduction(context);
        });
    }

    public static void ConfigureSerilog(this IHostBuilder builder)
    {
        builder.UseSerilog((context, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext();
        });
    }
}