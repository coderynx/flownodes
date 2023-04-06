using System.Collections.Immutable;
using Flownodes.Sdk.Entities;

namespace Flownodes.Sdk.Resourcing;

public abstract record ResourceContext(FlownodesId Id, ImmutableDictionary<string, object?> Metadata);

public sealed record DataSourceContext(
    FlownodesId Id,
    ImmutableDictionary<string, object?> Metadata,
    ImmutableDictionary<string, object?> Configuration
) : ResourceContext(Id, Metadata);

public sealed record DeviceContext(
    FlownodesId Id,
    ImmutableDictionary<string, object?> Metadata,
    ImmutableDictionary<string, object?> Configuration,
    ImmutableDictionary<string, object?> State
) : ResourceContext(Id, Metadata);