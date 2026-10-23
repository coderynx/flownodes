using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces.GetNamespace;

/// <summary>
/// Reads one Namespace.
/// </summary>
/// <param name="Name">The name of the Namespace. A name that is not valid names no Namespace.</param>
public sealed record Query(string Name) : IRequest<Result<Namespace>>;

internal sealed class Handler(IApplicationDbContext database) : IRequestHandler<Query, Result<Namespace>>
{
    public async Task<Result<Namespace>> HandleAsync(Query request, CancellationToken cancellationToken = default)
    {
        if (!NamespaceName.TryCreate(request.Name, out var name))
        {
            return Errors.NotFound(request.Name);
        }

        var found = await database.Namespaces.AsNoTracking()
            .SingleOrDefaultAsync(@namespace => @namespace.Name == name, cancellationToken);

        return found is null ? Errors.NotFound(request.Name) : Result.Found(found);
    }
}
