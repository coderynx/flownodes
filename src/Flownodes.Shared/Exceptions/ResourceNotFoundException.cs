namespace Flownodes.Shared.Exceptions;

public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string tenantName, string resourceName) :
        base($"The resource {resourceName} of tenant {tenantName} was not found")
    {
        TenantName = tenantName;
        ResourceName = resourceName;
    }

    public string TenantName { get; }
    public string ResourceName { get; }
}