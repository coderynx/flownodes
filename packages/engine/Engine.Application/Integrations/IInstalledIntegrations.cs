using Engine.Domain.Integrations;

namespace Engine.Application.Integrations;

/// <summary>
/// The Integrations this Silo found in the deployment, as loaded at startup.
/// </summary>
public interface IInstalledIntegrations
{
    /// <summary>
    /// Gets every Integration found, loaded or not, ordered by folder name.
    /// </summary>
    IReadOnlyList<InstalledIntegration> All { get; }
}
