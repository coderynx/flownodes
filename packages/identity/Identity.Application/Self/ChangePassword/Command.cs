using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Self.ChangePassword;

/// <summary>
/// Changes the caller's own password, which they must know.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="CurrentPassword">The password the caller has now.</param>
/// <param name="NewPassword">The new password, validated against the password policy.</param>
public sealed record Command(Guid CallerId, string CurrentPassword, string NewPassword) : IRequest<Result>;

internal sealed class Handler(IIdentityDbContext database, UserManager<User> users) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var caller = await Callers.RequireActiveAsync(database, request.CallerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        var user = caller.Value;
        if (!await users.CheckPasswordAsync(user, request.CurrentPassword))
        {
            return Errors.WrongCurrentPassword();
        }

        var validation = await PasswordPolicy.ValidateAsync(users, user, request.NewPassword);
        if (validation is not null)
        {
            return validation;
        }

        user.SetPasswordHash(users.PasswordHasher.HashPassword(user, request.NewPassword));
        await database.SaveChangesAsync(cancellationToken);

        return Result.Updated();
    }
}
