using Registry.Application.Resources;

namespace Registry.Presentation.Resources.Contracts;

internal sealed record DeleteResponse(bool DryRun, IReadOnlyList<DeletedDto> Resources);

internal sealed record DeletedDto(string Kind, string? Namespace, string Name);

internal static class DeletedMappings
{
    public static DeletedDto ToDto(this DeletedResource resource) =>
        new(resource.Kind, resource.Namespace, resource.Name);
}
