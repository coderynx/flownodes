namespace Engine.Infrastructure.Namespaces;

internal interface INamespaceGrain : IGrainWithStringKey
{
    Task ApplyAsync(Guid uid, long seq);
    Task DeleteAsync(Guid uid, long seq);
    Task ObserveAsync(long seq);
    Task ResourceAppliedAsync(string kind, string name, Guid uid, long generation, long seq, string spec);
    Task ResourceDeletedAsync(string kind, string name, Guid uid, long seq);
    Task WakeAsync();
}
