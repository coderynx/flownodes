using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Engine.Infrastructure.Flows;

internal sealed class FlowExecutionRetention(IFlowExecutions executions, ILogger<FlowExecutionRetention> logger)
    : BackgroundService
{
    private const int Kept = 100;
    private static readonly TimeSpan Retained = TimeSpan.FromDays(7);
    private static readonly TimeSpan Period = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period);
        do
        {
            try
            {
                await executions.PurgeAsync(DateTimeOffset.UtcNow - Retained, Kept, stoppingToken);
            }
            catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
            {
                logger.LogWarning(exception, "Flow Executions could not be purged");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
