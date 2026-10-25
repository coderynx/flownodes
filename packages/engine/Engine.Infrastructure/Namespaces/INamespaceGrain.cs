namespace Engine.Infrastructure.Namespaces;

internal interface INamespaceGrain : IGrainWithStringKey
{
    Task ApplyAsync(Guid uid, long seq);
    Task DeleteAsync(Guid uid, long seq);
    Task ObserveAsync(long seq);
}
