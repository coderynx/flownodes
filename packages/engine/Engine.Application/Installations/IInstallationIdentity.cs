using Engine.Domain.Installations;

namespace Engine.Application.Installations;

/// <summary>
/// Supplies the Installation's identifier, read from the Engine's stored state when the Silo starts.
/// </summary>
public interface IInstallationIdentity
{
    /// <summary>
    /// Gets the Installation.
    /// </summary>
    Installation Current { get; }
}
