using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Namespaces;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Namespaces.RemoveNamespace;

/// <summary>
/// Forgets a Namespace the Registry deleted and drops every Role Grant in it.
/// </summary>
/// <param name="Name">The name of the Namespace.</param>
/// <param name="Uid">The identifier of the incarnation that was deleted.</param>
/// <param name="Seq">The sequence number of the message.</param>
public sealed record Command(string Name, Guid Uid, long Seq) : IRequest<Result>;

internal sealed class Handler(IIdentityDbContext database) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        if (request.Name == KnownNamespace.Default)
        {
            return Result.Found();
        }

        var known = await database.KnownNamespaces.SingleOrDefaultAsync(
            candidate => candidate.Name == request.Name,
            cancellationToken);
        if (known is not null && known.IsDeletedBy(request.Uid, request.Seq))
        {
            database.KnownNamespaces.Remove(known);
        }

        var holders = await database.Users.Include(user => user.RoleGrants)
            .Where(user => user.RoleGrants.Any(grant => grant.Namespace == request.Name))
            .ToListAsync(cancellationToken);
        foreach (var holder in holders)
        {
            foreach (var revocation in holder.RevokeAllIn(request.Name))
            {
                await database.PublishAsync(holder.ToRoleRevoked(request.Name, revocation));
            }
        }

        await database.SaveChangesAsync(cancellationToken);

        return Result.Deleted();
    }
}
