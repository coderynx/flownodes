namespace Flownodes.Edge.Core.Resources;

/// <summary>
///     The device grain is a resource that represents the "digital twin" of a specific device.
/// </summary>
public interface IDeviceGrain : IGrainWithStringKey
{
    Task<ResourceIdentityCard> GetIdentityCard();
    Task PerformAction(string actionName, Dictionary<string, object?>? parameters = null);

    Task ConfigureAsync(string behaviorId, ResourceConfiguration configuration,
        Dictionary<string, string>? metadata = null);

    Task<object?> GetStateProperty(string key);
    Task<Dictionary<string, object?>> GetStateProperties();
    Task<string> GetFrn();
    Task SelfRemoveAsync();
}