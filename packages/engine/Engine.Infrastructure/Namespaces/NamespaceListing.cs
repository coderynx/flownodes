using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Infrastructure.Namespaces;

internal sealed class NamespaceListing(
    IRegistryRequests registry,
    IOptions<NamespaceListOptions> options,
    TimeProvider time,
    ILogger<NamespaceListing> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval, time);
        do
        {
            try
            {
                await registry.ListNamespacesAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
            {
                logger.LogWarning(exception, "The Namespaces could not be requested from the Registry");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
