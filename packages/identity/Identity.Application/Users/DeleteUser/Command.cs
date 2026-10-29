using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Application.Security;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Users.DeleteUser;

/// <summary>
/// Deletes a User, revoking their refresh tokens and Personal Access Tokens at once. Only an Administrator can, and
/// never the last active Administrator.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="UserId">The id of the User to delete.</param>
public sealed record Command(Guid CallerId, Guid UserId) : IRequest<Result>;

internal sealed class Handler(IIdentityDbContext database, ICallerChecks checks, IUserTokenRevoker revoker)
    : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var found = await checks.RequireAdministratorAndFindUserAsync(
            request.CallerId,
            request.UserId,
            cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        var user = found.Value;

        var isLastActiveAdministrator = user.IsAdministrator && !user.IsDisabled &&
                                        !await checks.AnotherActiveAdministratorExistsAsync(user.Id, cancellationToken);
        if (isLastActiveAdministrator)
        {
            return Errors.LastAdministrator();
        }

        await revoker.RevokeAllAsync(user.Id, cancellationToken);

        var tokens = await database.PersonalAccessTokens.Where(token => token.UserId == user.Id)
            .ToListAsync(cancellationToken);
        database.PersonalAccessTokens.RemoveRange(tokens);

        user.Delete();
        database.Users.Remove(user);
        database.DeletedUsers.Add(DeletedUser.Create(user));

        await database.PublishAsync(user.ToDeleted());
        await database.SaveChangesAsync(cancellationToken);

        return Result.Deleted();
    }
}
