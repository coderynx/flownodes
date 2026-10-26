using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Registry.Domain.Resources;

namespace Registry.Application.Resources.DeleteResources;

/// <summary>
/// Deletes every Resource a Manifest or a list of references names, or none of them (ADR-0007). A Resource another
/// one still references is not deleted unless the referrer is in the same batch, and nothing cascades. A namespaced
/// Resource needs the Engineer role in its Namespace; a Namespace needs an Administrator and must end up empty.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Documents">
/// The Resources to delete, each with at least <c>kind</c> and <c>metadata.name</c>; a Spec is ignored.
/// </param>
/// <param name="Namespace">
/// The Namespace the caller is working in, as for an apply; <see langword="null"/> for <c>default</c> and no check.
/// </param>
/// <param name="DryRun">Runs every check and reports what would be deleted, without deleting anything.</param>
public sealed record Command(Guid CallerId, IReadOnlyList<JsonElement> Documents, string? Namespace, bool DryRun)
    : IRequest<Result<IReadOnlyList<DeletedResource>>>;

internal sealed class Handler(KindCatalog catalog, ResourceDeletion deletion)
    : IRequestHandler<Command, Result<IReadOnlyList<DeletedResource>>>
{
    public Task<Result<IReadOnlyList<DeletedResource>>> HandleAsync(
        Command request,
        CancellationToken cancellationToken = default) =>
        deletion.DeleteAsync(
            request.CallerId,
            ManifestReader.ForDelete(request.Documents, request.Namespace, catalog),
            request.Documents.Count,
            request.DryRun,
            cancellationToken);
}
