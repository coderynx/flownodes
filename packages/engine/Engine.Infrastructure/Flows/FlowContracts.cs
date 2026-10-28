using System.Text.Json.Nodes;
using Engine.Infrastructure.Devices;
using Flownodes.Sdk;

namespace Engine.Infrastructure.Flows;

[GenerateSerializer]
[Immutable]
internal sealed record WatchResult([property: Id(0)] bool Exists, [property: Id(1)] DataPointSnapshot? Snapshot)
{
    public static WatchResult Missing { get; } = new(false, null);
}

[GenerateSerializer]
internal enum RunOutcome
{
    Started,
    Queued,
    Dropped,
    FlowMissing,
    TriggerUnknown
}

[GenerateSerializer]
[Immutable]
internal sealed record RunResult([property: Id(0)] RunOutcome Outcome, [property: Id(1)] Guid? Execution);

[GenerateSerializer]
internal sealed class FlowState
{
    [Id(0)] public Dictionary<string, bool> Conditions { get; set; } = [];
    [Id(1)] public Dictionary<string, DateTimeOffset> Scheduled { get; set; } = [];
    [Id(2)] public List<QueuedTrigger> Queue { get; set; } = [];
    [Id(3)] public LastResult? Last { get; set; }
}

[GenerateSerializer]
[Immutable]
internal sealed record QueuedTrigger([property: Id(0)] string Trigger, [property: Id(1)] string? Payload);

[GenerateSerializer]
[Immutable]
internal sealed record LastResult(
    [property: Id(0)] Guid Execution,
    [property: Id(1)] string State,
    [property: Id(2)] DateTimeOffset FinishedAt);

internal static class Readings
{
    public static JsonNode From(DataPointSnapshot snapshot) =>
        new JsonObject
        {
            ["value"] = snapshot.Value is null ? null : JsonNode.Parse(snapshot.Value),
            ["quality"] = ((Quality)snapshot.Quality).ToString().ToLowerInvariant(),
            ["timestamp"] = snapshot.Timestamp
        };

    public static bool IsUsable(DataPointSnapshot snapshot) =>
        snapshot.Value is not null && snapshot.Quality != (int)Quality.Bad;
}
