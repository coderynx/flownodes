using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Users.ResetPassword;

/// <summary>
/// Replaces a User's password without knowing the old one. Only an Administrator can.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="UserId">The id of the User whose password is reset.</param>
/// <param name="Password">The new password, validated against the password policy.</param>
public sealed record Command(Guid CallerId, Guid UserId, string Password) : IRequest<Result>;

internal sealed class Handler(IIdentityDbContext database, UserManager<User> users) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var caller = await Callers.RequireAdministratorAsync(database, request.CallerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        var user = await database.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == request.UserId,
            cancellationToken);
        if (user is null)
        {
            return Errors.UserNotFound();
        }

        var validation = await PasswordPolicy.ValidateAsync(users, user, request.Password);
        if (validation is not null)
        {
            return validation;
        }

        user.SetPasswordHash(users.PasswordHasher.HashPassword(user, request.Password));
        user.ResetFailedAccess();
        user.LockOutUntil(null);
        await database.SaveChangesAsync(cancellationToken);

        return Result.Updated();
    }
}
