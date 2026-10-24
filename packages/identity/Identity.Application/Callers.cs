using Coderynx.Functional.Results;
using Identity.Application.Database;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application;

internal static class Callers
{
    public static async Task<Result<User>> RequireActiveAsync(
        IIdentityDbContext database,
        Guid callerId,
        CancellationToken cancellationToken)
    {
        var caller = await database.Users.Include(user => user.RoleGrants)
            .SingleOrDefaultAsync(user => user.Id == callerId, cancellationToken);

        return caller is { IsDisabled: false } ? Result.Found(caller) : Errors.Unauthenticated();
    }

    public static async Task<Result<User>> RequireAdministratorAsync(
        IIdentityDbContext database,
        Guid callerId,
        CancellationToken cancellationToken)
    {
        var caller = await RequireActiveAsync(database, callerId, cancellationToken);

        return caller.IsSuccess && !caller.Value.IsAdministrator ? Errors.Forbidden() : caller;
    }

    public static Task<bool> AnotherActiveAdministratorExistsAsync(
        IIdentityDbContext database,
        Guid userId,
        CancellationToken cancellationToken) =>
        database.Users.AnyAsync(
            user => user.Id != userId && user.IsAdministrator && !user.IsDisabled,
            cancellationToken);
}
