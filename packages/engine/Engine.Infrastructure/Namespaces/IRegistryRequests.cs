namespace Engine.Infrastructure.Namespaces;

internal interface IRegistryRequests
{
    Task ResyncNamespaceAsync(string name, CancellationToken cancellationToken);
}
