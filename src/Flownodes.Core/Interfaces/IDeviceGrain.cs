using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

/// <summary>
///     The device grain is a resource that represents the "digital twin" of a specific device.
/// </summary>
public interface IDeviceGrain : IGrainWithStringKey
{
    Task<ResourceIdentityCard> GetIdentityCard();

    Task SetupAsync(string behaviourId, ResourceConfiguration configuration,
        Dictionary<string, string>? metadata = null);

    Task<object?> GetStateProperty(string key);
    Task<ResourceState> GetState();
    Task<string> GetFrn();

    Task RemoveAsync();

    /// <summary>
    ///     Gets the resource configuration.
    /// </summary>
    /// <returns>The resource configuration.</returns>
    ValueTask<ResourceConfiguration> GetConfiguration();

    /// <summary>
    ///     Updates the state of the device by storing the new state and applying it.
    /// </summary>
    /// <param name="newState">The new state of the device to store.</param>
    Task UpdateStateAsync(Dictionary<string, object?> newState);

    /// <summary>
    ///     Gets the device metadata.
    /// </summary>
    /// <returns>The device metadata</returns>
    ValueTask<Dictionary<string, string>> GetMetadata();
}