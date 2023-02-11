namespace Flownodes.Shared.Models;

public class ObjectId
{
    public ObjectId(string tenantId, string clusterId, string resourceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);
        ArgumentException.ThrowIfNullOrEmpty(clusterId);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        TenantName = tenantId;
        ClusterName = clusterId;
        ObjectName = resourceName;
    }

    public ObjectId(string id)
    {
        var tokens = id.Split('/');
        TenantName = tokens[0];
        ClusterName = tokens[1];
        ObjectName = tokens[2];
    }
    
    public string TenantName { get; }
    public string ClusterName { get; }
    public string ObjectName { get; }

    public override string ToString()
    {
        return $"{TenantName}/{ClusterName}/{ObjectName}";
    }
}