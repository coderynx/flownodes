namespace Flownodes.Shared.Models;

public record FlownodesId
{
    public FlownodesId(string tenantName, string resourceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        TenantName = tenantName;
        ResourceName = resourceName;
    }

    public FlownodesId(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        var tokens = id.Split('/');
        if (tokens.Length != 2) throw new ArgumentException($"The provided string {nameof(id)} is invalid.");

        TenantName = tokens[0];
        ResourceName = tokens[1];
    }

    public string Id => $"{TenantName}/{ResourceName}";
    public string TenantName { get; }
    public string ResourceName { get; }

    public override string ToString()
    {
        return Id;
    }

    public static implicit operator string(FlownodesId flownodesId)
    {
        return flownodesId.Id;
    }

    public static implicit operator FlownodesId(string flownodesId)
    {
        return new FlownodesId(flownodesId);
    }
}