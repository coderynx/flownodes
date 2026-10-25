namespace Engine.Infrastructure.Namespaces;

[GenerateSerializer]
internal sealed class NamespaceState
{
    [Id(0)] public Guid Uid { get; set; }
    [Id(1)] public long Seq { get; set; }
    [Id(2)] public bool Deleted { get; set; }
}
