namespace Flownodes.Shared.Resourcing.Exceptions;

public class ResourceNotStatefulException : Exception
{
    public ResourceNotStatefulException(string tenantName, string kind, string resourceName) : base(
        $"The resource {resourceName} of tenant {tenantName} is not stateful")
    {
        TenantName = tenantName;
        Kind = kind;
        ResourceName = resourceName;
    }

    public string TenantName { get; }
    public string Kind { get; }
    public string ResourceName { get; }
}