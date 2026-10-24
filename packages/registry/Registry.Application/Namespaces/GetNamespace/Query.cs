using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Application.Authorization;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces.GetNamespace;

/// <summary>
/// Reads one Namespace. The caller must hold a role in it; anyone else is told it does not exist.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Name">The name of the Namespace. A name that is not valid names no Namespace.</param>
public sealed record Query(Guid CallerId, string Name) : IRequest<Result<Namespace>>;

internal sealed class Handler(IApplicationDbContext database) : IRequestHandler<Query, Result<Namespace>>
{
    public async Task<Result<Namespace>> HandleAsync(Query request, CancellationToken cancellationToken = default)
    {
        var caller = await database.RequireMemberAsync(request.CallerId, request.Name, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        if (!NamespaceName.TryCreate(request.Name, out var name))
        {
            return Errors.NotFound(request.Name);
        }

        var found = await database.Namespaces.AsNoTracking()
            .SingleOrDefaultAsync(@namespace => @namespace.Name == name, cancellationToken);

        return found is null ? Errors.NotFound(request.Name) : Result.Found(found);
    }
}
