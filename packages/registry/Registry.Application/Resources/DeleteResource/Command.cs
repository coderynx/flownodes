using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Microsoft.Extensions.Options;
using Registry.Application.Database;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Application.Resources.DeleteResource;

/// <summary>
/// Deletes one Resource, under the same rules as a batch delete of one.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Resource, or <see langword="null"/> when deleting a Namespace.</param>
/// <param name="Kinds">The Kind as a route writes it, lowercase and plural, such as <c>connections</c>.</param>
/// <param name="Name">The name of the Resource. A name that is not valid names no Resource.</param>
/// <param name="DryRun">Runs every check without deleting anything.</param>
public sealed record Command(Guid CallerId, string? Namespace, string Kinds, string Name, bool DryRun)
    : IRequest<Result<DeletedResource>>;

internal sealed class Handler(
    IApplicationDbContext database,
    KindCatalog catalog,
    TimeProvider time,
    IOptions<RevisionRetention> retention) : IRequestHandler<Command, Result<DeletedResource>>
{
    public async Task<Result<DeletedResource>> HandleAsync(
        Command request,
        CancellationToken cancellationToken = default)
    {
        var target = Target(request);
        if (target.IsFailure)
        {
            var access = await database.AuthoriseChangesAsync(request.CallerId, Scope(request), cancellationToken);

            return access.IsFailure ? access.Error : target.Error;
        }

        var deleted = await new ResourceDeletion(database, catalog, time, retention.Value.Keep).DeleteAsync(
            request.CallerId,
            ManifestReading.Of(target.Value),
            1,
            request.DryRun,
            cancellationToken);

        return deleted.IsFailure ? deleted.Error : Result.Deleted(deleted.Value[0]);
    }

    private Result<ManifestEntry> Target(Command request)
    {
        if (request.Namespace is null && KindRoutes.IsNamespace(request.Kinds))
        {
            return ResourceName.TryCreate(request.Name, out var name)
                ? Result.Found(Entry(Namespace.Kind, name, NamespaceName.Create(name.Value), null))
                : Errors.NamespaceNotFound(request.Name);
        }

        var rules = catalog.FindByRoute(request.Kinds);
        if (rules is null || request.Namespace is null)
        {
            return Errors.KindNotFound(request.Kinds);
        }

        if (!NamespaceName.TryCreate(request.Namespace, out var @namespace))
        {
            return Errors.NamespaceNotFound(request.Namespace);
        }

        return ResourceName.TryCreate(request.Name, out var resourceName)
            ? Result.Found(Entry(rules.Name, resourceName, @namespace, rules))
            : Errors.NotFound(rules.Name, request.Name);
    }

    private static ManifestReading Scope(Command request)
    {
        var isNamespace = request.Namespace is null && KindRoutes.IsNamespace(request.Kinds);
        var namespaces = NamespaceName.TryCreate(request.Namespace, out var name)
            ? [name.Value]
            : Array.Empty<string>();

        return new ManifestReading([], [], namespaces, isNamespace);
    }

    private static ManifestEntry Entry(
        string kind,
        ResourceName name,
        NamespaceName @namespace,
        IResourceKind? rules) =>
        new(0, kind, name, @namespace, null, Namespace.Spec, rules);
}
