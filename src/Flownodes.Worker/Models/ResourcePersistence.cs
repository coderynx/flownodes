using Flownodes.Shared.Models;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
public class ResourcePersistence
{
    [Id(1)] public DateTime CreatedAt { get; set; } = DateTime.Now;
    [Id(2)] public ResourceConfigurationStore ConfigurationStore { get; set; } = new();
    [Id(3)] public Dictionary<string, string?> Metadata { get; set; } = new();
    [Id(4)] public ResourceStateStore StateStore { get; set; } = new();
}