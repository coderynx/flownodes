using Core.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Authorization;

/// <summary>
/// A module's database as far as authorisation goes: its replica of Users and their Role Grants. A module's own
/// database abstraction implements it, so the checks in <see cref="Access"/> run against that module's replica and no
/// other.
/// </summary>
public interface IUserAccessSource
{
    /// <summary>
    /// Gets the module's replica of Users and their Role Grants.
    /// </summary>
    DbSet<UserAccess> UserAccess { get; }

    /// <summary>
    /// Commits the changes tracked since the last save.
    /// </summary>
    /// <param name="cancellationToken">Cancels the commit.</param>
    /// <returns>A task that completes when the changes have committed.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
