namespace Flownodes.Sdk.Resourcing;

/// <summary>
///     The context of the target resource in the cluster.
/// </summary>
public sealed class ResourceContext
{
    public ResourceContext(string serviceId, string clusterId, FlownodesId id, DateTime createdAt, string? behaviourId,
        Dictionary<string, object?>? configuration, DateTime? configurationLastUpdateDate,
        Dictionary<string, string?> metadata, DateTime? metadataLastUpdateDate, Dictionary<string, object?>? state,
        DateTime? stateLastUpdateDate)
    {
        ServiceId = serviceId;
        ClusterId = clusterId;

        Id = id;
        if (Id.IsManager) throw new ArgumentException($"Provided invalid {nameof(FlownodesId)}", nameof(id));

        CreatedAt = createdAt;
        BehaviourId = behaviourId;
        Configuration = configuration;
        ConfigurationLastUpdateDate = configurationLastUpdateDate;
        Metadata = metadata;
        MetadataLastUpdateDate = metadataLastUpdateDate;
        State = state;
        StateLastUpdateDate = stateLastUpdateDate;
    }

    public string ServiceId { get; }
    public string ClusterId { get; }
    public FlownodesId Id { get; }
    public FlownodesObject ObjectKind => Id.ObjectKind;
    public string TenantName => Id.FirstName;
    public string ResourceName => Id.SecondName ?? throw new InvalidOperationException("Provided invalid FlownodesId");
    public DateTime CreatedAt { get; }
    public string? BehaviourId { get; }
    public Dictionary<string, object?>? Configuration { get; }
    public DateTime? ConfigurationLastUpdateDate { get; }
    public Dictionary<string, string?> Metadata { get; }
    public DateTime? MetadataLastUpdateDate { get; }
    public Dictionary<string, object?>? State { get; }
    public DateTime? StateLastUpdateDate { get; }
}