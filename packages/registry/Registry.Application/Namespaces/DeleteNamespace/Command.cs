using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Application.Authorization;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces.DeleteNamespace;

/// <summary>
/// Deletes a Namespace that is not the <c>default</c> one and holds no Resources. Only an Administrator can.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Name">The name of the Namespace. A name that is not valid names no Namespace.</param>
public sealed record Command(Guid CallerId, string Name) : IRequest<Result>;

internal sealed class Handler(IApplicationDbContext database) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var caller = await database.RequireAdministratorAsync(request.CallerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        if (!NamespaceName.TryCreate(request.Name, out var name))
        {
            return Errors.NotFound(request.Name);
        }

        var existing = await database.Namespaces.SingleOrDefaultAsync(
            @namespace => @namespace.Name == name,
            cancellationToken);
        if (existing is null)
        {
            return Errors.NotFound(request.Name);
        }

        if (name.IsDefault)
        {
            return Errors.DefaultCannotBeDeleted();
        }

        if (await database.Resources.AnyAsync(resource => resource.Namespace == name, cancellationToken))
        {
            return Errors.HoldsResources(request.Name);
        }

        existing.Delete();
        database.Namespaces.Remove(existing);

        await database.PublishAsync(existing.ToDeleted());
        await database.SaveChangesAsync(cancellationToken);

        return Result.Deleted();
    }
}
