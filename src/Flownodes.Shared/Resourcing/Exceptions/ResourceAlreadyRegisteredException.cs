namespace Flownodes.Shared.Resourcing.Exceptions;

public class ResourceAlreadyRegisteredException : Exception
{
    public ResourceAlreadyRegisteredException(string tenantNane, string resourceName) : base(
        $"The resource {resourceName} of tenant {tenantNane} is already registered")
    {
        TenantName = tenantNane;
        ResourceName = resourceName;
    }

    public string TenantName { get; }
    public string ResourceName { get; }
}