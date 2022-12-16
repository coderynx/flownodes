using Autofac;
using Autofac.Extensions.DependencyInjection;
using Serilog;

namespace Flownodes.Worker;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(ConfigureContainer)
            .ConfigureAppConfiguration(ConfigureAppConfiguration)
            .UseOrleans(ConfigureOrleans)
            .UseSerilog(ConfigureLogging)
            .Build();

        await host.RunAsync();
    }
}