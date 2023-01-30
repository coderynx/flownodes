namespace Flownodes.Shared.Models;

public class ResourceId
{
    private readonly string _id;

    public ResourceId(string id)
    {
        _id = id;
    }

    public ResourceId(string tenantId, string clusterId, string resourceName)
    {
        _id = $"{tenantId}/{clusterId}/{resourceName}";
    }

    public string TenantId => _id.Split('/')[0];
    public string ClusterId => _id.Split('/')[1];
    public string ResourceName => _id.Split('/')[2];

    public static explicit operator string(ResourceId id)
    {
        return id.ToString();
    }

    public static explicit operator ResourceId(string id)
    {
        return new ResourceId(id);
    }

    public override string ToString()
    {
        return _id;
    }
}