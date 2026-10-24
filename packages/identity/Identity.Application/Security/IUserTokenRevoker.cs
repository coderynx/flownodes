namespace Identity.Application.Security;

/// <summary>
/// Revokes the tokens Identity's token server issued to a User.
/// </summary>
public interface IUserTokenRevoker
{
    /// <summary>
    /// Revokes every refresh token and authorization of a User, so none of them can be exchanged again.
    /// </summary>
    /// <param name="userId">The id of the User, which tokens carry as their subject.</param>
    /// <param name="cancellationToken">Cancels the revocation.</param>
    /// <returns>A task that completes when every token is revoked.</returns>
    Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default);
}
