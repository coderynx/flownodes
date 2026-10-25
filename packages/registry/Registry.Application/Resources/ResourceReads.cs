using Coderynx.Functional.Results;
using Core.Application.Authorization;
using Registry.Application.Database;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal static class ResourceReads
{
    public static async Task<Result<(IResourceKind Kind, NamespaceName Namespace)>> AuthoriseReadAsync(
        this IApplicationDbContext database,
        KindCatalog catalog,
        Guid callerId,
        string @namespace,
        string kinds,
        CancellationToken cancellationToken)
    {
        var caller = await database.RequireMemberAsync(callerId, @namespace, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        var kind = catalog.FindByRoute(kinds);
        if (kind is null)
        {
            return Errors.KindNotFound(kinds);
        }

        return NamespaceName.TryCreate(@namespace, out var name)
            ? Result.Found((kind, name))
            : Errors.NamespaceNotFound(@namespace);
    }
}
