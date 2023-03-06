namespace Flownodes.Sdk.Resourcing;

/// <summary>
///     The context of the target resource in the cluster.
/// </summary>
public sealed class ResourceContext
{
    public ResourceContext(string serviceId, string clusterId, string tenantName, string resourceName,
        DateTime createdAt, string? behaviourId, Dictionary<string, object?> configuration,
        Dictionary<string, string?> metadata, Dictionary<string, object?> state)
    {
        ServiceId = serviceId;
        ClusterId = clusterId;
        TenantName = tenantName;
        ResourceName = resourceName;
        CreatedAt = createdAt;
        BehaviourId = behaviourId;
        Configuration = configuration;
        Metadata = metadata;
        State = state;
    }

    public string ServiceId { get; init; }
    public string ClusterId { get; init; }
    public string TenantName { get; init; }
    public string ResourceName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? BehaviourId { get; init; }
    public Dictionary<string, object?> Configuration { get; set; }
    public Dictionary<string, string?> Metadata { get; set; }
    public Dictionary<string, object?> State { get; set; }
    public DateTime? LastStateUpdate { get; init; }
}