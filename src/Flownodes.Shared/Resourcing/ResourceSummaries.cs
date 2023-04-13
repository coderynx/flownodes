using Flownodes.Sdk.Entities;

namespace Flownodes.Shared.Resourcing;

public interface IResourceSummary
{
    FlownodesId Id { get; }
    Dictionary<string, object?> Metadata { get; }
}

[GenerateSerializer]
public sealed record AssetSummary(
    [property: Id(0)] FlownodesId Id,
    [property: Id(1)] Dictionary<string, object?> Metadata,
    [property: Id(2)] Dictionary<string, object?> State
) : IResourceSummary;

[GenerateSerializer]
public sealed record DataSourceSummary(
    [property: Id(0)] FlownodesId Id,
    [property: Id(1)] Dictionary<string, object?> Metadata,
    [property: Id(2)] Dictionary<string, object?> Configuration
) : IResourceSummary;

[GenerateSerializer]
public sealed record DeviceSummary(
    [property: Id(0)] FlownodesId Id,
    [property: Id(1)] Dictionary<string, object?> Metadata,
    [property: Id(2)] string? BehaviourId,
    [property: Id(3)] Dictionary<string, object?> Configuration,
    [property: Id(4)] Dictionary<string, object?> State
) : IResourceSummary;

[GenerateSerializer]
public sealed record ResourceGroupSummary(
    [property: Id(0)] FlownodesId Id,
    [property: Id(1)] Dictionary<string, object?> Metadata,
    [property: Id(2)] HashSet<string> Registrations
) : IResourceSummary;

[GenerateSerializer]
public sealed record ScriptSummary(
    [property: Id(0)] FlownodesId Id,
    [property: Id(1)] Dictionary<string, object?> Metadata,
    [property: Id(2)] string? Code
) : IResourceSummary;