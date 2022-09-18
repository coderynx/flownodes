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
                var redisConnectionString = $"{Environment.GetEnvironmentVariable("REDIS")}:6379";

                var configurationConnectionString = host.Configuration.GetConnectionString("Redis");
                if (configurationConnectionString is not null) redisConnectionString = configurationConnectionString;

                builder
                    .ConfigureServices(services =>
                    {
                        services.AddOptions();
                        services.AddWorkflow(options =>
                        {
                            options.UseRedisPersistence(redisConnectionString, "flownodes");
                            options.UseRedisLocking(redisConnectionString, "flownodes");
                            options.UseRedisQueues(redisConnectionString, "flownodes");
                            options.UseRedisEventHub(redisConnectionString, "flownodes");
                        });
                        services.AddWorkflowDSL();
                        services.AddTransient<LoggerStep>();
                    });

                if (Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") is not null)
                {
                    builder.UseKubernetesHosting();
                }
                else
                {
                    builder.ConfigureEndpoints(11111, 30000);
                    builder.Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = host.Configuration["ClusterConfiguration:ClusterId"];
                        options.ServiceId = host.Configuration["ClusterConfiguration:ServiceId"];
                    });
                }

                builder
                    .UseRedisClustering(options =>
                    {
                        options.ConnectionString = redisConnectionString;
                        options.Database = 0;
                    })
                    .AddRedisGrainStorage("flownodes", options =>
                    {
                        options.ConnectionString = redisConnectionString;
                        options.DatabaseNumber = 1;
                        options.UseJson = true;
                    })
                    .AddRedisGrainStorageAsDefault(options =>
                    {
                        options.ConnectionString = redisConnectionString;
                        options.DatabaseNumber = 2;
                        options.UseJson = true;
                    })
                    .UseDashboard();
            })
            .UseSerilog((hostingContext, _, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.FromLogContext())
            .Build();

        await host.RunAsync();
    }
}