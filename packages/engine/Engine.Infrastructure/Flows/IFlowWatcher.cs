using Engine.Infrastructure.Devices;
using Orleans.Concurrency;

namespace Engine.Infrastructure.Flows;

internal interface IFlowWatcher : IGrainWithStringKey
{
    [OneWay]
    Task DataPointChangedAsync(string kind, string resource, DataPointSnapshot snapshot);

    [OneWay]
    Task EventReportedAsync(string device, string name, string payload, DateTimeOffset timestamp);
}
