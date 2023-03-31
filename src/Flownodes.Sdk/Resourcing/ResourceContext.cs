using System.Collections.Immutable;
using Flownodes.Sdk.Entities;

namespace Flownodes.Sdk.Resourcing;

/// <summary>
///     The context of the target resource in the cluster.
/// </summary>
public sealed class ResourceContext
{
    private readonly ImmutableDictionary<string, object?>? _configuration;
    private readonly ImmutableDictionary<string, object?>? _state;

    public ResourceContext(string serviceId, string clusterId, FlownodesId id, DateTime createdAt, string? behaviourId,
        bool isConfigurable, Dictionary<string, object?>? configuration, DateTime? configurationLastUpdateDate,
        Dictionary<string, string?> metadata, DateTime? metadataLastUpdateDate, bool isStateful,
        Dictionary<string, object?>? state,
        DateTime? stateLastUpdateDate)
    {
        ServiceId = serviceId;
        ClusterId = clusterId;

        Id = id;
        if (Id.IsManager) throw new ArgumentException($"Provided invalid {nameof(FlownodesId)}", nameof(id));

        CreatedAt = createdAt;
        BehaviourId = behaviourId;
        IsConfigurable = isConfigurable;
        _configuration = configuration?.ToImmutableDictionary();
        ConfigurationLastUpdateDate = configurationLastUpdateDate;
        Metadata = metadata.ToImmutableDictionary();
        MetadataLastUpdateDate = metadataLastUpdateDate;
        IsStateful = isStateful;
        _state = state?.ToImmutableDictionary();
        StateLastUpdateDate = stateLastUpdateDate;
    }

    public string ServiceId { get; }
    public string ClusterId { get; }
    public FlownodesId Id { get; }
    public FlownodesEntity EntityKind => Id.EntityKind;
    public string TenantName => Id.FirstName;
    public string ResourceName => Id.SecondName ?? throw new InvalidOperationException("Provided invalid FlownodesId");
    public DateTime CreatedAt { get; }
    public string? BehaviourId { get; }

    public bool IsConfigurable { get; }

    public ImmutableDictionary<string, object?>? Configuration
    {
        get
        {
            if (IsConfigurable && _configuration is not null) return _configuration;
            throw new InvalidOperationException("Resource is not configurable");
        }
    }

    public DateTime? ConfigurationLastUpdateDate { get; }

    public ImmutableDictionary<string, string?> Metadata { get; }
    public DateTime? MetadataLastUpdateDate { get; }

    public bool IsStateful { get; }

    public ImmutableDictionary<string, object?> State
    {
        get
        {
            if (IsStateful && _state is not null) return _state;
            throw new InvalidOperationException("Resource is not stateful");
        }
    }

    public DateTime? StateLastUpdateDate { get; }
}