using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Engine.Infrastructure.Flows;

internal sealed class FlowCancellationSweep(
    IFlowExecutions executions,
    IGrainFactory grains,
    ILogger<FlowCancellationSweep> logger) : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                foreach (var key in await executions.ListCancellingAsync(stoppingToken))
                {
                    await grains.GetGrain<IFlowExecutionGrain>(key).CancelAsync();
                }
            }
            catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
            {
                logger.LogWarning(exception, "Cancelled Flow Executions could not be swept");
            }
        }
    }
}
