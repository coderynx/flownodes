namespace Flownodes.Core.Models;

[GenerateSerializer]
public record ResourceSummary([property: Id(0)] string Id,
    [property: Id(1)] DateTime CreatedAt, [property: Id(2)] ResourceConfiguration? Configuration,
    [property: Id(3)] Dictionary<string, string?>? Metadata,
    [property: Id(4)] ResourceState? State);