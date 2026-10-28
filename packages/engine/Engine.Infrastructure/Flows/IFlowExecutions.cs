using Engine.Application.Flows;
using Engine.Domain.Flows;

namespace Engine.Infrastructure.Flows;

internal interface IFlowExecutions
{
    Task<bool> SaveAsync(FlowExecution execution, CancellationToken cancellationToken = default);
    Task<FlowExecution?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListActiveAsync(
        string @namespace,
        string flow,
        Guid flowUid,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> RequestCancelAsync(
        string @namespace,
        string flow,
        Guid flowUid,
        CancellationToken cancellationToken = default);

    Task<FlowExecutionPage> PageAsync(
        string @namespace,
        string flow,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);

    Task PurgeAsync(DateTimeOffset finishedBefore, int keep, CancellationToken cancellationToken = default);
}
