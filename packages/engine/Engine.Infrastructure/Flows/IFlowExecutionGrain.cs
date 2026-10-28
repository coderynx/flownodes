namespace Engine.Infrastructure.Flows;

internal interface IFlowExecutionGrain : IFlowWatcher
{
    Task PingAsync();
    Task<bool> CancelAsync();
}
