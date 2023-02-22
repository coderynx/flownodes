namespace Flownodes.Shared.Models;

[GenerateSerializer]
public record Resource(
    [property: Id(0)] string Id,
    [property: Id(1)] DateTime CreatedAt,
    [property: Id(2)] Dictionary<string, object?> Configuration,
    [property: Id(3)] string? BehaviorId,
    [property: Id(4)] Dictionary<string, string?> Metadata,
    [property: Id(5)] Dictionary<string, object?>? State,
    [property: Id(6)] DateTime? LastUpdate
);