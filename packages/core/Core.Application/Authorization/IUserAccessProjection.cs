using Core.Domain.Authorization;

namespace Core.Application.Authorization;

/// <summary>
/// Builds a module's replica of Users and Role Grants from Identity's messages (ADR-0026).
/// </summary>
public interface IUserAccessProjection
{
    /// <summary>
    /// Changes the replica of one User, starting it when Identity has not reported the User before, and saves it. The
    /// replica itself drops a change whose message is not newer than the last applied.
    /// </summary>
    /// <param name="userId">The immutable id of the User.</param>
    /// <param name="change">Applies one of Identity's messages to the replica.</param>
    /// <param name="cancellationToken">Cancels the update.</param>
    /// <returns>A task that completes when the replica is saved.</returns>
    Task ApplyAsync(Guid userId, Action<UserAccess> change, CancellationToken cancellationToken = default);
}
