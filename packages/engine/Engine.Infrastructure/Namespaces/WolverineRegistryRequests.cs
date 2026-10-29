using Core.Contracts.Resources;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Engine.Infrastructure.Namespaces;

internal sealed class WolverineRegistryRequests(IServiceScopeFactory scopes) : IRegistryRequests
{
    public Task ListNamespacesAsync(CancellationToken cancellationToken) =>
        PublishAsync(new NamespacesListRequested());

    public Task ResyncNamespaceAsync(string name, CancellationToken cancellationToken) =>
        PublishAsync(new NamespaceResyncRequested(name));

    private async Task PublishAsync<TMessage>(TMessage message) where TMessage : notnull
    {
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>().PublishAsync(message);
    }
}
