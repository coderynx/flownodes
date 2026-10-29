using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Users.ResyncUsers;

/// <summary>
/// Publishes Users again, each as one message with their whole state, because a module asked for it (ADR-0026).
/// Deleted Users are published from their tombstones, and a User nothing was ever published about is skipped. It acts
/// for a module, not for a caller.
/// </summary>
/// <param name="UserId">The id of the one User to publish, or <see langword="null"/> for every User.</param>
public sealed record Command(Guid? UserId) : IRequest<Result>;

internal sealed class Handler(IIdentityDbContext database) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var users = database.Users.AsNoTracking().Include(user => user.RoleGrants).Where(user => user.Seq > 0);
        var deleted = database.DeletedUsers.AsNoTracking();
        if (request.UserId is { } userId)
        {
            users = users.Where(user => user.Id == userId);
            deleted = deleted.Where(user => user.Id == userId);
        }

        foreach (var user in await users.ToListAsync(cancellationToken))
        {
            await database.PublishAsync(user.ToAccessPublished());
        }

        foreach (var user in await deleted.ToListAsync(cancellationToken))
        {
            await database.PublishAsync(user.ToAccessPublished());
        }

        await database.SaveChangesAsync(cancellationToken);

        return Result.Found();
    }
}
