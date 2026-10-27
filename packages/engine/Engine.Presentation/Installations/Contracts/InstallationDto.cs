using Engine.Domain.Installations;

namespace Engine.Presentation.Installations.Contracts;

internal sealed record InstallationDto(Guid Id, string Version);

internal static class InstallationMappings
{
    public static InstallationDto ToDto(this Installation installation) =>
        new(installation.Id, installation.Version);
}
