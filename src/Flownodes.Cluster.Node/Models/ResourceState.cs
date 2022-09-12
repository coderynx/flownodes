namespace Flownodes.Cluster.Node.Models;

public class ResourceState
{
    public DateTime? LastUpdate { get; set; }
    public Dictionary<string, object?> Properties { get; } = new();

    public object? GetPropertyValue(string key)
    {
        return !Properties.ContainsKey(key) ? null : Properties[key];
    }
}