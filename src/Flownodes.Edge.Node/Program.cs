using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Edge.Node.Automation;
using Flownodes.Edge.Node.Modules;
using Microsoft.AspNetCore.Mvc.Diagnostics;
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

                var configurationConnectionString = host.Configuration.GetConnectionString("redis");
                if (configurationConnectionString is not null) redisConnectionString = configurationConnectionString;

                builder
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<IBehaviorProvider, BehaviorProvider>();
                        
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
                        services.AddHostedService<TestWorker>();
                    });
                
                if (Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") is not null)
                {
                    builder.UseKubernetesHosting();
                }

                else
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