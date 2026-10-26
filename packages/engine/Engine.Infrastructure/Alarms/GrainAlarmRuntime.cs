using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Engine.Application.Alarms;
using Engine.Domain.Alarms;
using Engine.Infrastructure.Assets;

namespace Engine.Infrastructure.Alarms;

internal sealed class GrainAlarmRuntime(IGrainFactory grains, IAlarmHistory history) : IAlarmRuntime
{
    public async Task<IReadOnlyList<Alarm>> ListLiveAsync(string @namespace, CancellationToken cancellationToken) =>
        await history.ListLiveAsync(@namespace, cancellationToken);

    public async Task<Result<IReadOnlyList<Alarm>>> ListAssetAsync(
        string @namespace,
        string asset,
        CancellationToken cancellationToken)
    {
        var snapshots = await grains.GetGrain<IAssetGrain>($"{@namespace}/{asset}").GetAlarmsAsync();
        if (snapshots is null)
        {
            return Error.NotFound("asset.not_found", $"Asset '{asset}' does not exist.");
        }

        return Result.Found<IReadOnlyList<Alarm>>(
        [
            .. snapshots.Select(snapshot => snapshot.ToAlarm())
                .OrderByDescending(alarm => alarm.Priority)
                .ThenBy(alarm => alarm.RaisedAt)
        ]);
    }

    public Task<AlarmHistoryPage> HistoryAsync(
        string @namespace,
        string? asset,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int offset,
        int limit,
        CancellationToken cancellationToken) =>
        history.PageAsync(@namespace, asset, from, to, offset, limit, cancellationToken);

    public async Task<Result> AcknowledgeAsync(
        string @namespace,
        Guid id,
        Guid by,
        string? comment,
        CancellationToken cancellationToken)
    {
        var asset = await history.FindLiveAssetAsync(@namespace, id, cancellationToken);
        var found = asset is not null &&
                    await grains.GetGrain<IAssetGrain>($"{@namespace}/{asset}").AcknowledgeAsync(id, by, comment);

        return found ? Result.Updated() : Error.NotFound("alarm.not_found", $"Alarm '{id}' is not live.");
    }
}
