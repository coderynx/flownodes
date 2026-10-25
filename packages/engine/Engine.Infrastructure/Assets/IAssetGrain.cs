using Engine.Infrastructure.Connections;
using Engine.Infrastructure.Devices;
using Orleans.Concurrency;

namespace Engine.Infrastructure.Assets;

internal interface IAssetGrain : IGrainWithStringKey
{
    Task ApplyAsync(Guid uid, long generation, long seq, string spec);
    Task DeleteAsync(Guid uid, long seq);
    Task PingAsync();
    Task AssetTypeChangedAsync(AssetTypeInfo assetType);

    [OneWay]
    Task DeviceChangedAsync(string device, DeviceFeed feed);

    Task<IReadOnlyList<DataPointSnapshot>?> GetDataPointsAsync();
    Task<CallResult> WriteAsync(string dataPoint, string value);
}
