namespace Flownodes.Shared.Models;

[GenerateSerializer]
public record TenantConfiguration
{
    [Id(0)] public Dictionary<string, string?> Metadata { get; set; } = new();
}