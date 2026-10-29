namespace Engine.Infrastructure.Namespaces;

internal interface IRegistryRequests
{
    Task ListNamespacesAsync(CancellationToken cancellationToken);
    Task ResyncNamespaceAsync(string name, CancellationToken cancellationToken);
}
