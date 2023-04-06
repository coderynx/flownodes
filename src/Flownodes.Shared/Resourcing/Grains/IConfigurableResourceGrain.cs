namespace Flownodes.Shared.Resourcing.Grains;

/// <summary>
///     The interface for defining resource with configuration.
/// </summary>
public interface IConfigurableResourceGrain : IResourceGrain
{
    /// <summary>
    ///     Gets the currently stored configuration of the resource.
    /// </summary>
    /// <returns>The current resource configuration.</returns>
    ValueTask<Dictionary<string, object?>> GetConfiguration();

    /// <summary>
    ///     Updates the resource configuration.
    /// </summary>
    /// <param name="configuration">The resource configuration to store.</param>
    Task UpdateConfigurationAsync(Dictionary<string, object?> configuration);

    /// <summary>
    ///     Updates the BehaviourId.
    /// </summary>
    /// <param name="behaviourId">The BehaviourId to set.</param>
    Task UpdateBehaviourId(string behaviourId);

    /// <summary>
    ///     Clears the resource configuration.
    /// </summary>
    Task ClearConfigurationAsync();
}