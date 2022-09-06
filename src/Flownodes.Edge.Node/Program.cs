using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Edge.Node.Modules;
using Orleans;
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
                builder
                    .ConfigureServices(services =>
                    {
                        services.AddOptions();
                        services.AddHostedService<Worker>();
                    })
                    .UseLocalhostClustering()
                    .AddMemoryGrainStorage("flownodes")
                    .UseDashboard();
            })
            .UseSerilog((hostingContext, _, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.FromLogContext())
            .Build();

        await host.RunAsync();
    }
}