using Engine.Infrastructure.Connections;
using Engine.Infrastructure.Flows;
using Orleans.Concurrency;

namespace Engine.Infrastructure.Devices;

internal interface IDeviceGrain : IGrainWithStringKey
{
    Task ApplyAsync(Guid uid, long generation, long seq, string spec);
    Task DeleteAsync(Guid uid, long seq);
    Task PingAsync();
    Task ConnectionChangedAsync(ConnectionInfo connection);

    [OneWay]
    Task ValueReportedAsync(string dataPoint, string value, DateTimeOffset timestamp, int quality);

    [OneWay]
    Task StateReportedAsync(int deviceState);

    [OneWay]
    Task EventReportedAsync(string name, string payload, DateTimeOffset timestamp);

    Task<IReadOnlyList<DataPointSnapshot>?> GetDataPointsAsync();
    Task<DeviceFeed> SubscribeAsync(string asset);
    Task UnsubscribeAsync(string asset);
    Task<WatchResult> WatchDataPointAsync(IFlowWatcher watcher, string dataPoint);
    Task<bool> WatchEventAsync(IFlowWatcher watcher, string name);
    Task UnwatchAsync(IFlowWatcher watcher);
    Task<CallResult> WriteAsync(string dataPoint, string value);
    Task<CallResult> InvokeAsync(string action, string parameters, string invocationId);
}
