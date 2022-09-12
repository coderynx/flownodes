using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Cluster.Node.Automation;
using Flownodes.Cluster.Node.Modules;
using Orleans;
using Orleans.Hosting;
using Serilog;

namespace Flownodes.Cluster.Node;

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
                var connectionString = $"{Environment.GetEnvironmentVariable("REDIS")}:6379";
                
                builder
                    .ConfigureServices(services =>
                    {
                        services.AddOptions();
                        services.AddWorkflow(options =>
                        {
                            options.UseRedisPersistence(connectionString, "flownodes");
                            options.UseRedisLocking(connectionString, "flownodes");
                            options.UseRedisQueues(connectionString, "flownodes");
                            options.UseRedisEventHub(connectionString, "flownodes");
                        });
                        services.AddWorkflowDSL();
                        services.AddTransient<LoggerStep>();
                        // services.AddHostedService<TestWorker>();
                    })
                    .UseKubernetesHosting()
                    .UseRedisClustering(options =>
                    {
                        options.ConnectionString = connectionString;
                        options.Database = 0;
                    })
                    .AddRedisGrainStorage("flownodes", options =>
                    {
                        options.ConnectionString = connectionString;
                        options.DatabaseNumber = 1;
                        options.UseJson = true;
                    })
                    .AddRedisGrainStorageAsDefault(options =>
                    {
                        options.ConnectionString = connectionString;
                        options.DatabaseNumber = 1;
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