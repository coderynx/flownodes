using Flownodes.Sdk.Entities;

namespace Flownodes.Shared.Resourcing;

[GenerateSerializer]
public sealed record ResourceSummary(
    [property: Id(0)] FlownodesId Id,
    [property: Id(1)] string? BehaviorId,
    [property: Id(2)] DateTime CreatedAt,
    [property: Id(3)] Dictionary<string, object?>? Configuration,
    [property: Id(4)] Dictionary<string, string?>? Metadata,
    [property: Id(5)] Dictionary<string, object?>? State,
    [property: Id(6)] DateTime? StateLastUpdate
);