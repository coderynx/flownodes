using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Engine.Infrastructure.Alarms;

internal sealed class AlarmRetention(IAlarmHistory history, ILogger<AlarmRetention> logger) : BackgroundService
{
    private static readonly TimeSpan Retained = TimeSpan.FromDays(30);
    private static readonly TimeSpan Period = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period);
        do
        {
            try
            {
                await history.PurgeAsync(DateTimeOffset.UtcNow - Retained, stoppingToken);
            }
            catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
            {
                logger.LogWarning(exception, "Alarm history could not be purged");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
