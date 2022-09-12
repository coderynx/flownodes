using ConsoleAppFramework;
using Flownodes.Cluster.CliClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddOptions();
    })
    .RunConsoleAppFrameworkAsync<GatewayClient>(args);