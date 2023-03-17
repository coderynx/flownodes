namespace Flownodes.Shared.Resourcing;

/// <summary>
///     The interface for defining resource with configuration.
/// </summary>
public interface IConfigurableResource : IResourceGrain
{
    /// <summary>
    ///     Gets the stored configuration of the resource.
    /// </summary>
    /// <returns>The current resource configuration.</returns>
    ValueTask<(Dictionary<string, object?> Configuration, DateTime? LastUpdateDate)> GetConfiguration();

    /// <summary>
    ///     Configures the resource.
    /// </summary>
    /// <param name="configuration">The resource configuration to store.</param>
    Task UpdateConfigurationAsync(Dictionary<string, object?> configuration);

    /// <summary>
    ///     Clears the resource configuration.
    /// </summary>
    /// <returns></returns>
    Task ClearConfigurationAsync();
}