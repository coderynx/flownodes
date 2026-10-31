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

    public async Task<Result<Alarm>> GetAsync(string @namespace, Guid id, CancellationToken cancellationToken)
    {
        var alarm = await history.FindAsync(@namespace, id, cancellationToken);

        return alarm is null ? Error.NotFound("alarm.not_found", $"Alarm '{id}' does not exist.") : Result.Found(alarm);
    }

    public Task<AlarmHistoryPage> HistoryAsync(
        string @namespace,
        string? asset,
        DateTimeOffset? from,
        DateTimeOffset? to,
        AlarmPriority? priority,
        AlarmState? state,
        int offset,
        int limit,
        CancellationToken cancellationToken) =>
        history.PageAsync(@namespace, asset, from, to, priority, state, offset, limit, cancellationToken);

    public async Task<Result> AcknowledgeAsync(
        string @namespace,
        Guid id,
        Guid by,
        string byName,
        string? comment,
        CancellationToken cancellationToken)
    {
        var alarm = await history.FindAsync(@namespace, id, cancellationToken);
        if (alarm is { IsEnded: false })
        {
            var grain = grains.GetGrain<IAssetGrain>($"{@namespace}/{alarm.Asset}");
            var acknowledged = await grain.AcknowledgeAsync(id, by, byName, comment);
            alarm = acknowledged?.ToAlarm() ?? await history.FindAsync(@namespace, id, cancellationToken);
        }

        if (alarm is not { IsAcknowledged: true })
        {
            return Error.NotFound("alarm.not_found", $"Alarm '{id}' is not live.");
        }

        return alarm.AcknowledgedBy == by
            ? Result.Updated()
            : Error.Conflict(
                "alarm.already_acknowledged",
                $"Alarm '{id}' was already acknowledged by {alarm.AcknowledgedByName}.");
    }
}
