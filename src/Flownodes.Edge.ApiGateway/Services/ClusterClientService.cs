using Orleans;

namespace Flownodes.Edge.ApiGateway.Services;

public class ClusterClientService : IHostedService
{
    public ClusterClientService(ILoggerProvider loggerProvider)
    {
        Client = new ClientBuilder()
            .UseLocalhostClustering()
            .ConfigureLogging(builder => builder.AddProvider(loggerProvider))
            .Build();
    }

    public IClusterClient Client { get; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Client.Connect();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Client.Close();
        Client.Dispose();
    }
}