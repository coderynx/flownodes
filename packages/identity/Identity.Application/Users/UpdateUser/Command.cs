using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Users.UpdateUser;

/// <summary>
/// Renames a User or changes their display name. Only an Administrator can.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="UserId">The id of the User to change.</param>
/// <param name="Username">The new username; unchanged when <see langword="null"/>.</param>
/// <param name="DisplayName">The new display name; unchanged when <see langword="null"/>, cleared when empty.</param>
public sealed record Command(Guid CallerId, Guid UserId, string? Username, string? DisplayName)
    : IRequest<Result<User>>;

internal sealed class Handler(IIdentityDbContext database) : IRequestHandler<Command, Result<User>>
{
    public async Task<Result<User>> HandleAsync(Command request, CancellationToken cancellationToken = default)
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

        if (request.DisplayName is not null)
        {
            user.ChangeDisplayName(request.DisplayName);
        }

        if (request.Username is not null)
        {
            if (!Username.TryCreate(request.Username, out var username))
            {
                return Errors.InvalidUsername(request.Username);
            }

            var taken = await database.Users.AnyAsync(
                other => other.Id != user.Id && other.Username == username,
                cancellationToken);
            if (taken)
            {
                return Errors.UsernameTaken(username.Value);
            }

            if (user.Rename(username))
            {
                await database.PublishAsync(user.ToRenamed());
            }
        }

        await database.SaveChangesAsync(cancellationToken);

        return Result.Updated(user);
    }
}
