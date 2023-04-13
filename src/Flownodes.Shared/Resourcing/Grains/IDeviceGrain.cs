namespace Flownodes.Shared.Resourcing.Grains;

/// <summary>
///     The device grain is a resource that represents the "digital twin" of a specific device.
/// </summary>
public interface IDeviceGrain : IResourceGrain
{
    /// <summary>
    ///     Updates the BehaviourId.
    /// </summary>
    /// <param name="behaviourId">The BehaviourId to set.</param>
    Task UpdateBehaviourId(string behaviourId);

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
    ///     Clears the resource configuration.
    /// </summary>
    Task ClearConfigurationAsync();

    /// <summary>
    ///     Updates the state of the resource by storing the new state and applying it.
    /// </summary>
    /// <param name="state">The state to store.</param>
    Task UpdateStateAsync(Dictionary<string, object?> state);

    /// <summary>
    ///     Gets the currently stored state of the resource.
    /// </summary>
    /// <returns>The stored state of the resource.</returns>
    ValueTask<Dictionary<string, object?>> GetState();

    /// <summary>
    ///     Clears the resource state.
    /// </summary>
    Task ClearStateAsync();
}