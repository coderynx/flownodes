using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;

namespace Identity.Application.Users.DisableUser;

/// <summary>
/// Disables a User and revokes their refresh tokens and Personal Access Tokens at once. Only an Administrator can,
/// and never the last active Administrator.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="UserId">The id of the User to disable.</param>
public sealed record Command(Guid CallerId, Guid UserId) : IRequest<Result>;

internal sealed class Handler(IIdentityDbContext database, ICallerChecks checks, IUserTokens tokens)
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

        await tokens.RevokeAllAsync(user.Id, cancellationToken);

        if (user.Disable())
        {
            await database.PublishAsync(user.ToDisabled());
        }

        await database.SaveChangesAsync(cancellationToken);

        return Result.Updated();
    }
}
