using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

public interface IResourceManagerGrain : IGrainWithStringKey
{
    Task<IDeviceGrain> RegisterDeviceAsync(string id, ResourceConfiguration configuration);

    Task<IAssetGrain> RegisterAssetAsync(string id);

    Task RemoveDeviceAsync(string id);
    Task RemoveAssetAsync(string id);

    Task<List<IDeviceGrain>> GetDevices();
    Task<IDeviceGrain?> GetDevice(string id);
    Task<List<IAssetGrain>> GetAssets();
    Task<IAssetGrain?> GetAsset(string id);
}