namespace Flownodes.Sdk.Resourcing;

/// <summary>
///     The context of the target resource in the cluster.
/// </summary>
public sealed class ResourceContext
{
    public ResourceContext(string serviceId, string clusterId, FlownodesId id,
        DateTime createdAt, string? behaviourId, Dictionary<string, object?>? configuration,
        Dictionary<string, string?> metadata, Dictionary<string, object?>? state, DateTime? lastStateUpdateDate)
    {
        ServiceId = serviceId;
        ClusterId = clusterId;
        Id = id;
        CreatedAt = createdAt;
        BehaviourId = behaviourId;
        Configuration = configuration;
        Metadata = metadata;
        State = state;
        LastStateUpdateDate = lastStateUpdateDate;
    }

    public string ServiceId { get; }
    public string ClusterId { get; }
    public FlownodesId Id { get; }
    public string TenantName => Id.FirstName;
    public string ResourceName => Id.SecondName ?? throw new InvalidOperationException("Provided invalid FlownodesId");
    public DateTime CreatedAt { get; }
    public string? BehaviourId { get; }
    public Dictionary<string, object?>? Configuration { get; }
    public Dictionary<string, string?> Metadata { get; }
    public Dictionary<string, object?>? State { get; }
    public DateTime? LastStateUpdateDate { get; }
}