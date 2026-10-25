using Engine.Domain.Integrations;

namespace Engine.Presentation.Integrations.Contracts;

internal sealed record IntegrationDto(
    string Folder,
    string? Id,
    string? Version,
    string? SdkVersion,
    IReadOnlyList<string> DeviceTypes,
    string Status,
    string? Reason,
    string? Message);

internal static class IntegrationMappings
{
    public static IntegrationDto ToDto(this InstalledIntegration integration) =>
        new(
            integration.Folder,
            integration.Id?.Value,
            integration.Version,
            integration.SdkVersion,
            integration.DeviceTypes,
            integration.IsLoaded ? "Loaded" : "Failed",
            integration.Failure?.ToString(),
            integration.Message);
}
