using Engine.Infrastructure.Alarms;
using Engine.Infrastructure.Connections;
using Engine.Infrastructure.Devices;
using Engine.Infrastructure.Flows;
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
    Task<IReadOnlyList<AlarmSnapshot>?> GetAlarmsAsync();
    Task<bool> AcknowledgeAsync(Guid id, Guid by, string? comment);
    Task<WatchResult> WatchDataPointAsync(IFlowWatcher watcher, string dataPoint);
    Task UnwatchAsync(IFlowWatcher watcher);
}
