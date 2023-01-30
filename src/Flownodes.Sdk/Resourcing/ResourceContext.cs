namespace Flownodes.Sdk.Resourcing;

/// <summary>
///     The context of the target resource in the cluster.
/// </summary>
public sealed class ResourceContext
{
    public string ServiceId { get; init; }
    public string ClusterId { get; init; }
    public string TenantId { get; init; }
    public string ResourceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? BehaviorId { get; init; }
    public Dictionary<string, object?> Configuration { get; set; }
    public Dictionary<string, string?> Metadata { get; set; }
    public Dictionary<string, object?> State { get; set; }
    public DateTime? LastStateUpdate { get; init; }
}