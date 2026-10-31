using Engine.Application.Alarms;
using Engine.Domain.Alarms;

namespace Engine.Infrastructure.Alarms;

internal interface IAlarmHistory
{
    Task UpsertAsync(string @namespace, IReadOnlyList<Alarm> alarms, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Alarm>> ListLiveAsync(string @namespace, CancellationToken cancellationToken = default);
    Task<Alarm?> FindAsync(string @namespace, Guid id, CancellationToken cancellationToken = default);

    Task<AlarmHistoryPage> PageAsync(
        string @namespace,
        string? asset,
        DateTimeOffset? from,
        DateTimeOffset? to,
        AlarmPriority? priority,
        AlarmState? state,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);

    Task PurgeAsync(DateTimeOffset endedBefore, CancellationToken cancellationToken = default);
}
