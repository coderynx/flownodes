using Serilog;

namespace Flownodes.Worker;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .UseOrleans(ConfigureOrleans)
            .UseSerilog(ConfigureLogging)
            .Build();

        await host.RunAsync();
    }
}