using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Resources;

namespace Registry.Application.Resources.GetResource;

/// <summary>
/// Reads one namespaced Resource with its Spec and Status. The caller must hold the Operator or Engineer role in its
/// Namespace; anyone else is told the Namespace does not exist.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The name of the Namespace.</param>
/// <param name="Kinds">The Kind as a route writes it, lowercase and plural, such as <c>connections</c>.</param>
/// <param name="Name">The name of the Resource. A name that is not valid names no Resource.</param>
public sealed record Query(Guid CallerId, string Namespace, string Kinds, string Name) : IRequest<Result<Resource>>;

internal sealed class Handler(IApplicationDbContext database, KindCatalog catalog)
    : IRequestHandler<Query, Result<Resource>>
{
    public async Task<Result<Resource>> HandleAsync(Query request, CancellationToken cancellationToken = default)
    {
        var read = await database.AuthoriseReadAsync(
            catalog,
            request.CallerId,
            request.Namespace,
            request.Kinds,
            cancellationToken);
        if (read.IsFailure)
        {
            return read.Error;
        }

        var (kind, @namespace) = read.Value;
        if (!ResourceName.TryCreate(request.Name, out var name))
        {
            return Errors.NotFound(kind.Name, request.Name);
        }

        var found = await database.Resources.AsNoTracking()
            .SingleOrDefaultAsync(
                resource => resource.Namespace == @namespace && resource.Kind == kind.Name && resource.Name == name,
                cancellationToken);

        return found is null ? Errors.NotFound(kind.Name, request.Name) : Result.Found(found);
    }
}
