namespace Flownodes.Shared.Models;

[GenerateSerializer]
public sealed record ResourceSummary(
    [property: Id(0)] string Id,
    [property: Id(1)] string TenantName,
    [property: Id(2)] string ResourceName,
    [property: Id(3)] string Kind,
    [property: Id(4)] string? BehaviorId,
    [property: Id(5)] DateTime CreatedAt,
    [property: Id(6)] Dictionary<string, object?>? Configuration,
    [property: Id(8)] Dictionary<string, string?>? Metadata,
    [property: Id(9)] Dictionary<string, object?>? State,
    [property: Id(10)] DateTime? LastUpdate
);