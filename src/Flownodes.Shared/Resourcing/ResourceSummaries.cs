using Flownodes.Sdk.Entities;

namespace Flownodes.Shared.Resourcing;

[GenerateSerializer]
public abstract record BaseResourceSummary(
    [property: Id(0)] FlownodesId Id,
    [property: Id(1)] Dictionary<string, object?> Metadata
);

[GenerateSerializer]
public sealed record AssetSummary(
    FlownodesId Id,
    Dictionary<string, object?> Metadata,
    [property: Id(0)] Dictionary<string, object?> State
) : BaseResourceSummary(Id, Metadata);

[GenerateSerializer]
public sealed record DataSourceSummary(
    FlownodesId Id,
    Dictionary<string, object?> Metadata,
    [property: Id(0)] Dictionary<string, object?> Configuration
) : BaseResourceSummary(Id, Metadata);

[GenerateSerializer]
public sealed record DeviceSummary(
    FlownodesId Id,
    Dictionary<string, object?> Metadata,
    [property: Id(0)] string? BehaviourId,
    [property: Id(1)] Dictionary<string, object?> Configuration,
    [property: Id(2)] Dictionary<string, object?> State
) : BaseResourceSummary(Id, Metadata);

[GenerateSerializer]
public sealed record ResourceGroupSummary(
    FlownodesId Id,
    Dictionary<string, object?> Metadata,
    [property: Id(0)] HashSet<string> Registrations
) : BaseResourceSummary(Id, Metadata);

[GenerateSerializer]
public sealed record ScriptSummary(
    FlownodesId Id,
    Dictionary<string, object?> Metadata,
    [property: Id(0)] string? Code
) : BaseResourceSummary(Id, Metadata);