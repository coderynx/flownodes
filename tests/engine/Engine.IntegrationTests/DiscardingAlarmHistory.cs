using Engine.Application.Alarms;
using Engine.Domain.Alarms;
using Engine.Infrastructure.Alarms;

namespace Engine.IntegrationTests;

internal sealed class DiscardingAlarmHistory : IAlarmHistory
{
    public Task UpsertAsync(
        string @namespace,
        IReadOnlyList<Alarm> alarms,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<Alarm>> ListLiveAsync(string @namespace, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Alarm>>([]);

    public Task<string?> FindLiveAssetAsync(
        string @namespace,
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    public Task<AlarmHistoryPage> PageAsync(
        string @namespace,
        string? asset,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int offset,
        int limit,
        CancellationToken cancellationToken = default) => Task.FromResult(new AlarmHistoryPage([], 0));

    public Task PurgeAsync(DateTimeOffset endedBefore, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
