using Flownodes.Shared.Models;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
public class ResourcePersistence
{
    [Id(0)] public ResourceConfigurationStore ConfigurationStore { get; set; } = new();
    [Id(1)] public ResourceMetadataStore Metadata { get; set; } = new();
    [Id(2)] public ResourceStateStore StateStore { get; set; } = new();
}