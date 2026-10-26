using Coderynx.Functional.Results;
using Identity.Application.Database;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application;

internal interface ICallerChecks
{
    Task<Result<User>> RequireActiveAsync(Guid callerId, CancellationToken cancellationToken);
    Task<Result<User>> RequireAdministratorAsync(Guid callerId, CancellationToken cancellationToken);
    Task<Result<User>> FindUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<User>> RequireAdministratorAndFindUserAsync(
        Guid callerId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> AnotherActiveAdministratorExistsAsync(Guid userId, CancellationToken cancellationToken);
}

internal sealed class CallerChecks(IIdentityDbContext database) : ICallerChecks
{
    public async Task<Result<User>> RequireActiveAsync(Guid callerId, CancellationToken cancellationToken)
    {
        var caller = await database.Users.Include(user => user.RoleGrants)
            .SingleOrDefaultAsync(user => user.Id == callerId, cancellationToken);

        return caller is { IsDisabled: false } ? Result.Found(caller) : Errors.Unauthenticated();
    }

    public async Task<Result<User>> RequireAdministratorAsync(Guid callerId, CancellationToken cancellationToken)
    {
        var caller = await RequireActiveAsync(callerId, cancellationToken);

        return caller.IsSuccess && !caller.Value.IsAdministrator ? Errors.Forbidden() : caller;
    }

    public async Task<Result<User>> FindUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await database.Users.Include(candidate => candidate.RoleGrants)
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        return user is null ? Errors.UserNotFound() : Result.Found(user);
    }

    public async Task<Result<User>> RequireAdministratorAndFindUserAsync(
        Guid callerId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var caller = await RequireAdministratorAsync(callerId, cancellationToken);

        return caller.IsFailure ? caller.Error : await FindUserAsync(userId, cancellationToken);
    }

    public Task<bool> AnotherActiveAdministratorExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        database.Users.AnyAsync(
            user => user.Id != userId && user.IsAdministrator && !user.IsDisabled,
            cancellationToken);
}
