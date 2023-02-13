namespace Flownodes.Shared.Models;

public class FlownodesId
{
    public FlownodesId(string tenantName, string resourceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        TenantName = tenantName;
        ObjectName = resourceName;
    }

    public FlownodesId(string id)
    {
        var tokens = id.Split('/');
        TenantName = tokens[0];
        ObjectName = tokens[1];
    }
    
    public string TenantName { get; }
    public string ObjectName { get; }

    public override string ToString()
    {
        return $"{TenantName}/{ObjectName}";
    }
}