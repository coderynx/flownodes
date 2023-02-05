using Throw;

namespace Flownodes.Shared.Models;

public class ObjectId
{
    public ObjectId(string tenantId, string clusterId, string resourceName)
    {
        tenantId.ThrowIfNull().IfWhiteSpace().IfEmpty();
        clusterId.ThrowIfNull().IfWhiteSpace().IfEmpty();
        clusterId.ThrowIfNull().IfWhiteSpace().IfEmpty();

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