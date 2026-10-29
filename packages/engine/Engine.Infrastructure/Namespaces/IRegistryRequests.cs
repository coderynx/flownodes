namespace Engine.Infrastructure.Namespaces;

internal interface IRegistryRequests
{
    Task RequestNamespaceListAsync(CancellationToken cancellationToken);
    Task ResyncNamespaceAsync(string name, CancellationToken cancellationToken);
}
