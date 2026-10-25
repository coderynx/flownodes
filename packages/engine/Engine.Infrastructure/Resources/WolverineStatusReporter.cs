using System.Text.Json;
using Core.Contracts.Resources;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Engine.Infrastructure.Resources;

internal sealed class WolverineStatusReporter(IServiceScopeFactory scopes) : IStatusReporter
{
    public async Task ReportAsync(
        string @namespace,
        string kind,
        string name,
        Guid uid,
        long observedGeneration,
        long statusSeq,
        JsonElement status)
    {
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(new StatusReported(@namespace, kind, name, uid, observedGeneration, statusSeq, status));
    }
}
