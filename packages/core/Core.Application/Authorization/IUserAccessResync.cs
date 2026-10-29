namespace Core.Application.Authorization;

/// <summary>
/// Asks Identity to publish Users again, each with their whole state, so the module's replica of Role Grants catches up
/// on messages it missed (ADR-0026). Each module that keeps a replica implements it with its own messaging.
/// </summary>
public interface IUserAccessResync
{
    /// <summary>
    /// Asks Identity to publish Users again. It does not wait for the answer.
    /// </summary>
    /// <param name="userId">The id of the one User to publish, or <see langword="null"/> for every User.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>A task that completes when the request is sent.</returns>
    Task RequestAsync(Guid? userId, CancellationToken cancellationToken = default);
}
