namespace Engine.Infrastructure.Namespaces;

[GenerateSerializer]
internal sealed class NamespaceState
{
    [Id(0)] public Guid Uid { get; set; }
    [Id(1)] public long Seq { get; set; }
    [Id(2)] public bool Deleted { get; set; }
    [Id(3)] public List<Member> Members { get; set; } = [];
}

[GenerateSerializer]
internal sealed record Member([property: Id(0)] string Kind, [property: Id(1)] string Name);
