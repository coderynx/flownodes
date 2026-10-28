using Engine.Infrastructure.Flows;
using Orleans.Concurrency;

namespace Engine.Infrastructure.Connections;

internal interface IConnectionGrain : IGrainWithStringKey
{
    Task ApplyAsync(Guid uid, long generation, long seq, string spec);
    Task DeleteAsync(Guid uid, long seq);
    Task PingAsync();
    Task<ConnectionInfo> RegisterDeviceAsync(DeviceRegistration device);
    Task UnregisterDeviceAsync(string name);
    Task<CallResult> WriteAsync(string device, string dataPoint, string value);
    Task<CallResult> InvokeAsync(string? device, string action, string parameters, string invocationId);
    Task<bool> WatchEventAsync(IFlowWatcher watcher, string name);
    Task UnwatchAsync(IFlowWatcher watcher);

    [OneWay]
    Task RuntimeConnectingAsync(long runtime);

    [OneWay]
    Task RuntimeConnectivityAsync(long runtime, int connectivity, string? message);

    [OneWay]
    Task RuntimeFailedAsync(long runtime, string message);

    [OneWay]
    Task RuntimeValueAsync(
        long runtime,
        string device,
        string dataPoint,
        string value,
        DateTimeOffset timestamp,
        int quality);

    [OneWay]
    Task RuntimeEventAsync(long runtime, string? device, string name, string payload, DateTimeOffset timestamp);

    [OneWay]
    Task RuntimeDeviceStateAsync(long runtime, string device, int deviceState);

    [OneWay]
    Task RuntimeDiscoveriesAsync(long runtime, string discoveries);
}
