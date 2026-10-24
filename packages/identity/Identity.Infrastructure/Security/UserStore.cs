using Identity.Domain.Users;
using Identity.Infrastructure.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Security;

internal sealed class UserStore(IdentityDbContext database) : IUserPasswordStore<User>, IUserLockoutStore<User>
{
    public void Dispose()
    {
    }

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Username.Value);

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        user.Rename(Username.Create(userName!));

        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Username.Normalized);

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        database.Users.Add(user);

        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(IdentityResult.Success);

    public Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        database.Users.Remove(user);

        return Task.FromResult(IdentityResult.Success);
    }

    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        Guid.TryParse(userId, out var id)
            ? await database.Users.Include(user => user.RoleGrants)
                .SingleOrDefaultAsync(user => user.Id == id, cancellationToken)
            : null;

    public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        Username.TryCreate(normalizedUserName, out var username)
            ? await database.Users.Include(user => user.RoleGrants)
                .SingleOrDefaultAsync(user => user.Username == username, cancellationToken)
            : null;

    public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.SetPasswordHash(passwordHash);

        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash is not null);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.LockoutEnd);

    public Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockOutUntil(lockoutEnd);

        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.RecordFailedAccess());

    public Task ResetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
    {
        user.ResetFailedAccess();

        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.AccessFailedCount);

    public Task<bool> GetLockoutEnabledAsync(User user, CancellationToken cancellationToken) => Task.FromResult(true);

    public Task SetLockoutEnabledAsync(User user, bool enabled, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
