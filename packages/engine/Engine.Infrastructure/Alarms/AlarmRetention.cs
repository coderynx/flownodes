using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Engine.Infrastructure.Alarms;

/// <summary>
/// Deletes the history of Alarms that ended more than 30 days ago.
/// </summary>
internal sealed class AlarmRetention(IAlarmHistory history, ILogger<AlarmRetention> logger) : BackgroundService
{
    public static readonly TimeSpan Retained = TimeSpan.FromDays(30);
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
