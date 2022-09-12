using Orleans;

namespace Flownodes.Cluster.Core.Resources;

public interface IResourceManagerGrain : IGrainWithStringKey
{
    Task<IDeviceGrain> RegisterDeviceAsync(string id, string behaviorId,
        Dictionary<string, object?>? configuration = null);

    Task<IDataCollectorGrain> RegisterDataCollectorAsync(string id, string behaviorId,
        Dictionary<string, object?>? configuration = null);

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