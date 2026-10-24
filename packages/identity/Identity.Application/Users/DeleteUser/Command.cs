using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Application.Security;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Users.DeleteUser;

/// <summary>
/// Deletes a User, revoking their refresh tokens and Personal Access Tokens at once. Only an Administrator can, and
/// never the last active Administrator.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="UserId">The id of the User to delete.</param>
public sealed record Command(Guid CallerId, Guid UserId) : IRequest<Result>;

internal sealed class Handler(IIdentityDbContext database, IUserTokenRevoker revoker) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var caller = await Callers.RequireAdministratorAsync(database, request.CallerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        var user = await database.Users.Include(candidate => candidate.RoleGrants)
            .SingleOrDefaultAsync(candidate => candidate.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return Errors.UserNotFound();
        }

        if (user.IsAdministrator && !user.IsDisabled &&
            !await Callers.AnotherActiveAdministratorExistsAsync(database, user.Id, cancellationToken))
        {
            return Errors.LastAdministrator();
        }

        await revoker.RevokeAllAsync(user.Id, cancellationToken);

        var tokens = await database.PersonalAccessTokens.Where(token => token.UserId == user.Id)
            .ToListAsync(cancellationToken);
        database.PersonalAccessTokens.RemoveRange(tokens);

        user.Delete();
        database.Users.Remove(user);

        await database.PublishAsync(user.ToDeleted());
        await database.SaveChangesAsync(cancellationToken);

        return Result.Deleted();
    }
}
