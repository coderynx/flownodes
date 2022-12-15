namespace Flownodes.Edge.Node;

public record EnvironmentOptions
{
    public string ResourceManagerName { get; set; } = "resource_manager";
    public string AlertManagerName { get; set; } = "alert_manager";
}