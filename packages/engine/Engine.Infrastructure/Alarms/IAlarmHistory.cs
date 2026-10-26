using Engine.Application.Alarms;
using Engine.Domain.Alarms;

namespace Engine.Infrastructure.Alarms;

/// <summary>
/// The Engine's record of every Alarm: one row per Alarm, updated at each transition and kept for 30 days once ended.
/// </summary>
internal interface IAlarmHistory
{
    Task UpsertAsync(string @namespace, IReadOnlyList<Alarm> alarms, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Alarm>> ListLiveAsync(string @namespace, CancellationToken cancellationToken = default);
    Task<string?> FindLiveAssetAsync(string @namespace, Guid id, CancellationToken cancellationToken = default);

    Task<AlarmHistoryPage> PageAsync(
        string @namespace,
        string? asset,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);

    Task PurgeAsync(DateTimeOffset endedBefore, CancellationToken cancellationToken = default);
}
