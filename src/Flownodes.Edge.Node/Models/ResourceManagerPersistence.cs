namespace Flownodes.Edge.Node.Models;

[GenerateSerializer]
public class ResourceManagerPersistence
{
    [Id(0)] public Dictionary<string, string?> ResourceRegistrations { get; set; } = new();
}