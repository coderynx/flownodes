using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Users.CreateUser;

/// <summary>
/// Creates a User with a password and no roles. Only an Administrator can.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Username">The username, validated by the handler.</param>
/// <param name="DisplayName">The optional display name.</param>
/// <param name="Password">The password, validated against the password policy.</param>
public sealed record Command(Guid CallerId, string Username, string? DisplayName, string Password)
    : IRequest<Result<User>>;

internal sealed class Handler(IIdentityDbContext database, UserManager<User> users)
    : IRequestHandler<Command, Result<User>>
{
    public async Task<Result<User>> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var caller = await CallerChecks.RequireAdministratorAsync(database, request.CallerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        if (!Username.TryCreate(request.Username, out var username))
        {
            return Errors.InvalidUsername(request.Username);
        }

        if (await users.FindByNameAsync(username.Value) is not null)
        {
            return Errors.UsernameTaken(username.Value);
        }

        var user = User.Create(username, request.DisplayName);
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return Errors.InvalidPassword(created.Errors.Select(error => error.Description));
        }

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Errors.UsernameTaken(username.Value);
        }

        return Result.Created(user);
    }
}
