using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

/// <summary>
///     The device grain is a resource that represents the "digital twin" of a specific device.
/// </summary>
public interface IDeviceGrain : IGrainWithStringKey
{
    Task<ResourceIdentityCard> GetIdentityCard();
    Task PerformAction(string id, Dictionary<string, object?>? parameters = null);

    Task SetupAsync(string behaviourId, ResourceConfiguration configuration,
        Dictionary<string, string>? metadata = null);

    Task<object?> GetStateProperty(string key);
    Task<Dictionary<string, object?>> GetStateProperties();
    Task<string> GetFrn();
    Task SelfRemoveAsync();

    /// <summary>
    ///     Updates the state of the device by storing the new state and applying it.
    /// </summary>
    /// <param name="newState">The new state of the device to store.</param>
    Task UpdateStateAsync(Dictionary<string, object?> newState);
}