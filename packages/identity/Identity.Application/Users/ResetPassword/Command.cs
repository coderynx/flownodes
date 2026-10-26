using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Users.ResetPassword;

/// <summary>
/// Replaces a User's password without knowing the old one and revokes their refresh tokens and Personal Access
/// Tokens, so whoever held the old password is signed out. Only an Administrator can.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="UserId">The id of the User whose password is reset.</param>
/// <param name="Password">The new password, validated against the password policy.</param>
public sealed record Command(Guid CallerId, Guid UserId, string Password) : IRequest<Result>;

internal sealed class Handler(
    IIdentityDbContext database,
    ICallerChecks checks,
    UserManager<User> users,
    IUserTokens tokens,
    IPasswordPolicy policy) : IRequestHandler<Command, Result>
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

        var validation = await policy.ValidateAsync(user, request.Password);
        if (validation is not null)
        {
            return validation;
        }

        await tokens.RevokeAllAsync(user.Id, cancellationToken);

        user.SetPasswordHash(users.PasswordHasher.HashPassword(user, request.Password));
        user.ResetFailedAccess();
        user.LockOutUntil(null);

        await database.SaveChangesAsync(cancellationToken);

        return Result.Updated();
    }
}
