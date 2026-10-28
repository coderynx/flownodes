using Orleans.Concurrency;

namespace Engine.Infrastructure.Flows;

internal interface IFlowGrain : IFlowWatcher
{
    Task ApplyAsync(Guid uid, long generation, long seq, string spec);
    Task DeleteAsync(Guid uid, long seq);
    Task PingAsync();
    Task<TriggerResult> TriggerAsync(string? trigger, string payload);
    Task<int?> CancelAllAsync();

    [OneWay]
    Task ExecutionFinishedAsync(Guid flowUid, Guid execution, string state, DateTimeOffset finishedAt);
}
