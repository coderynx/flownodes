using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

public interface IResourceManagerGrain : IGrainWithStringKey
{
    Task<IDeviceGrain> RegisterDeviceAsync(string id, string behaviourId,
        ResourceConfiguration? configuration = null);

    Task<IDataCollectorGrain> RegisterDataCollectorAsync(string id, string behaviorId,
        ResourceConfiguration? configuration = null);

    Task<IAssetGrain> RegisterAssetAsync(string id);

    Task RemoveDeviceAsync(string id);
    Task RemoveDataCollectorAsync(string id);
    Task RemoveAssetAsync(string id);

    Task<List<IDeviceGrain>> GetDevices();
    Task<IDeviceGrain?> GetDevice(string id);
    Task<List<IDataCollectorGrain>> GetDataCollectors();
    Task<IDataCollectorGrain?> GetDataCollector(string id);
    Task<List<IAssetGrain>> GetAssets();
    Task<IAssetGrain?> GetAsset(string id);
}