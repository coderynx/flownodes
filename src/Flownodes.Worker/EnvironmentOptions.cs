namespace Flownodes.Worker;

public record EnvironmentOptions
{
    public string ResourceManagerName { get; set; } = "resource_manager";
    public string AlertManagerName { get; set; } = "alert_manager";
    public string TenantManagerName { get; set; } = "tenant_manager";
}