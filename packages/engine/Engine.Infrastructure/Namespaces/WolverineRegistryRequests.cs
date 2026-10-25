using Core.Contracts.Resources;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Engine.Infrastructure.Namespaces;

internal sealed class WolverineRegistryRequests(IServiceScopeFactory scopes) : IRegistryRequests
{
    public async Task ResyncNamespaceAsync(string name, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>().PublishAsync(new NamespacesResyncRequested(name));
    }
}
