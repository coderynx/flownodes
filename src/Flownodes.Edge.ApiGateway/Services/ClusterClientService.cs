using Orleans;
using Orleans.Configuration;

namespace Flownodes.Edge.ApiGateway.Services;

public class ClusterClientService : IHostedService
{
    public ClusterClientService(ILoggerProvider loggerProvider, IConfiguration configuration)
    {
        Client = new ClientBuilder()
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = Environment.GetEnvironmentVariable("CLUSTER-ID");
                options.ServiceId = Environment.GetEnvironmentVariable("SERVICE-ID");
            })
            .UseRedisClustering(options =>
            {
                options.ConnectionString = configuration.GetConnectionString("redis");
                options.Database = 0;
            })
            .ConfigureLogging(builder => builder.AddProvider(loggerProvider))
            .Build();
    }

    public IClusterClient Client { get; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;
        const int maxAttempts = 5;
        
        await Client.Connect(async exception =>
        {
            attempt++;
            Console.WriteLine($"Cluster client attempt {attempt} of {maxAttempts} failed to connect to cluster.  Exception: {exception}");
            if (attempt > maxAttempts)
            {
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken);
            return true;
        });
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Client.Close();
        Client.Dispose();
    }
}