using Engine.Domain.Alarms;

namespace Engine.Infrastructure.Alarms;

[GenerateSerializer]
internal sealed class AlarmState
{
    [Id(0)] public List<AlarmSnapshot> Live { get; set; } = [];
    [Id(1)] public Dictionary<string, HoldingSnapshot> Pending { get; set; } = [];

    public static AlarmState From(AssetAlarms alarms) =>
        new()
        {
            Live = [.. alarms.Live.Select(AlarmSnapshot.From)],
            Pending = alarms.Pending.ToDictionary(
                entry => entry.Key,
                entry => new HoldingSnapshot(entry.Value.Since, entry.Value.Trigger))
        };

    public AssetAlarms ToAlarms(string asset) =>
        AssetAlarms.Restore(
            asset,
            Live.Select(snapshot => snapshot.ToAlarm()),
            Pending.ToDictionary(entry => entry.Key, entry => new Holding(entry.Value.Since, entry.Value.Trigger)));
}

[GenerateSerializer]
[Immutable]
internal sealed record HoldingSnapshot([property: Id(0)] DateTimeOffset Since, [property: Id(1)] string Trigger);

[GenerateSerializer]
[Immutable]
internal sealed record AlarmSnapshot(
    [property: Id(0)] Guid Id,
    [property: Id(1)] string Asset,
    [property: Id(2)] string Condition,
    [property: Id(3)] string Trigger,
    [property: Id(4)] DateTimeOffset RaisedAt,
    [property: Id(5)] int Priority,
    [property: Id(6)] string Message,
    [property: Id(7)] int ActiveCount,
    [property: Id(8)] DateTimeOffset? AcknowledgedAt,
    [property: Id(9)] Guid? AcknowledgedBy,
    [property: Id(10)] string? AcknowledgementComment,
    [property: Id(11)] DateTimeOffset? ClearedAt)
{
    public static AlarmSnapshot From(Alarm alarm) =>
        new(
            alarm.Id,
            alarm.Asset,
            alarm.Condition,
            alarm.Trigger,
            alarm.RaisedAt,
            (int)alarm.Priority,
            alarm.Message,
            alarm.ActiveCount,
            alarm.AcknowledgedAt,
            alarm.AcknowledgedBy,
            alarm.AcknowledgementComment,
            alarm.ClearedAt);

    public Alarm ToAlarm() =>
        Alarm.Restore(
            Id,
            Asset,
            Condition,
            Trigger,
            RaisedAt,
            (AlarmPriority)Priority,
            Message,
            ActiveCount,
            AcknowledgedAt,
            AcknowledgedBy,
            AcknowledgementComment,
            ClearedAt,
            null,
            null);
}
