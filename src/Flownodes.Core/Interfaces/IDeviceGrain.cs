namespace Flownodes.Core.Interfaces;

/// <summary>
///     The device grain is a resource that represents the "digital twin" of a specific device.
/// </summary>
public interface IDeviceGrain : IResourceGrain
{
    /// <summary>
    ///     Updates the state of the device by storing the new state and applying it.
    /// </summary>
    /// <param name="newState">The new state of the device to store.</param>
    Task UpdateStateAsync(Dictionary<string, object?> newState);
}