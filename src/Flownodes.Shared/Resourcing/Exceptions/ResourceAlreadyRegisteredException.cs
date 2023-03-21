namespace Flownodes.Shared.Resourcing.Exceptions;

[GenerateSerializer]
public class ResourceAlreadyRegisteredException : Exception
{
    public ResourceAlreadyRegisteredException(string tenantNane, string resourceName) : base(
        $"The resource {resourceName} of tenant {tenantNane} is already registered")
    {
        TenantName = tenantNane;
        ResourceName = resourceName;
    }

    [Id(0)] public string TenantName { get; }
    [Id(1)] public string ResourceName { get; }
}