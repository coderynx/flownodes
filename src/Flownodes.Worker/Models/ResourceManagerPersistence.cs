namespace Flownodes.Worker.Models;

[GenerateSerializer]
public record ResourceRegistration
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string Kind { get; set; }
    [Id(2)] public string Frn { get; set; }
}

[GenerateSerializer]
public class ResourceManagerPersistence
{
    [Id(0)] public List<ResourceRegistration> Registrations { get; set; } = new();
}