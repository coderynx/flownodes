using Core.Domain.Authorization;

namespace Engine.Infrastructure.Authorization;

[GenerateSerializer]
internal sealed class UserAccessState
{
    [Id(0)] public long Seq { get; set; }
    [Id(1)] public bool Disabled { get; set; }
    [Id(2)] public bool Deleted { get; set; }
    [Id(3)] public List<GrantState> Grants { get; set; } = [];
}

[GenerateSerializer]
internal sealed record GrantState([property: Id(0)] string Namespace, [property: Id(1)] NamespaceRole Role);
