namespace Flownodes.Shared.Resourcing;

[GenerateSerializer]
public sealed record ResourceSummary(
    [property: Id(0)] string Id,
    [property: Id(1)] Dictionary<string, object?> Metadata,
    [property: Id(2)] Dictionary<string, object?> Properties
);