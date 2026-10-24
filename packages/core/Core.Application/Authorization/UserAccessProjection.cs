using Core.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Authorization;

/// <summary>
/// Builds a module's replica of Users and Role Grants from Identity's messages (ADR-0026).
/// </summary>
public static class UserAccessProjection
{
    /// <summary>
    /// Changes the replica of one User, starting it when Identity has not reported the User before, and saves it. The
    /// replica itself drops a change whose message is not newer than the last applied.
    /// </summary>
    /// <param name="source">The module's database.</param>
    /// <param name="userId">The immutable id of the User.</param>
    /// <param name="change">Applies one of Identity's messages to the replica.</param>
    /// <param name="cancellationToken">Cancels the update.</param>
    /// <returns>A task that completes when the replica is saved.</returns>
    public static async Task ApplyAsync(
        this IUserAccessSource source,
        Guid userId,
        Action<UserAccess> change,
        CancellationToken cancellationToken = default)
    {
        var user = await source.UserAccess.Include(candidate => candidate.Grants)
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            user = UserAccess.Create(userId);
            source.UserAccess.Add(user);
        }

        change(user);

        await source.SaveChangesAsync(cancellationToken);
    }
}
