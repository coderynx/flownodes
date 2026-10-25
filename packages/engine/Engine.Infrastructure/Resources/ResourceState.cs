namespace Engine.Infrastructure.Resources;

[GenerateSerializer]
internal sealed class ResourceState
{
    [Id(0)] public Guid Uid { get; set; }
    [Id(1)] public long Generation { get; set; }
    [Id(2)] public long Seq { get; set; }
    [Id(3)] public string? Spec { get; set; }
    [Id(4)] public long StatusSeq { get; set; }
    [Id(5)] public bool Deleted { get; set; }
}
