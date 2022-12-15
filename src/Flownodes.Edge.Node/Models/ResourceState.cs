namespace Flownodes.Edge.Node.Models;

[GenerateSerializer]
public class ResourceState
{
    [Id(0)] public DateTime? LastUpdate { get; set; }
    [Id(1)] public Dictionary<string, object?> Properties { get; } = new();

    public object? GetPropertyValue(string key)
    {
        return !Properties.ContainsKey(key) ? null : Properties[key];
    }
}