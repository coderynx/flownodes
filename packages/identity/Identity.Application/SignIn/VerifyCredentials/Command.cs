using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.SignIn.VerifyCredentials;

/// <summary>
/// Checks a username and password for the sign-in page. Wrong passwords count towards the lockout.
/// </summary>
/// <param name="Username">The username as typed.</param>
/// <param name="Password">The password as typed.</param>
public sealed record Command(string Username, string Password) : IRequest<Result<User>>;

internal sealed class Handler(IIdentityDbContext database, UserManager<User> users)
    : IRequestHandler<Command, Result<User>>
{
    public async Task<Result<User>> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByNameAsync(request.Username);
        if (user is null || user.IsDisabled)
        {
            return Errors.InvalidCredentials();
        }

        if (await users.IsLockedOutAsync(user))
        {
            return Errors.LockedOut();
        }

        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            await database.SaveChangesAsync(cancellationToken);

            return await users.IsLockedOutAsync(user) ? Errors.LockedOut() : Errors.InvalidCredentials();
        }

        await users.ResetAccessFailedCountAsync(user);
        await database.SaveChangesAsync(cancellationToken);

        return Result.Found(user);
    }
}
