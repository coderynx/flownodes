namespace Identity.Domain.Tokens;

/// <summary>
/// A long-lived secret a User creates to sign in without a browser, exchanged for short-lived access tokens. Only a
/// hash of the secret is stored.
/// </summary>
public sealed class PersonalAccessToken
{
    private PersonalAccessToken()
    {
    }

    /// <summary>
    /// Gets the id that names the token in routes; it is not part of the secret.
    /// </summary>
    public required Guid Id { get; init; }
    /// <summary>
    /// Gets the id of the User who owns the token.
    /// </summary>
    public required Guid UserId { get; init; }
    /// <summary>
    /// Gets the name the User gave the token to recognise it.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// Gets the hash of the secret.
    /// </summary>
    public required string SecretHash { get; init; }
    /// <summary>
    /// Gets the moment the token was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
    /// <summary>
    /// Gets the moment the token stops working; <see langword="null"/> when it never expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }
    /// <summary>
    /// Gets the moment the token was revoked; <see langword="null"/> while it is not.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Creates a Personal Access Token.
    /// </summary>
    /// <param name="userId">The id of the User who owns the token.</param>
    /// <param name="name">The name the User gave the token.</param>
    /// <param name="secretHash">The hash of the secret.</param>
    /// <param name="now">The moment of creation.</param>
    /// <param name="expiresAt">The moment the token stops working, or <see langword="null"/> for never.</param>
    /// <returns>The new token.</returns>
    public static PersonalAccessToken Create(
        Guid userId,
        string name,
        string secretHash,
        DateTimeOffset now,
        DateTimeOffset? expiresAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            SecretHash = secretHash,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };

    /// <summary>
    /// Determines whether the token can be exchanged at the given moment.
    /// </summary>
    /// <param name="now">The moment to check.</param>
    /// <returns><see langword="true"/> when the token is neither revoked nor expired.</returns>
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && (ExpiresAt is null || ExpiresAt > now);

    /// <summary>
    /// Revokes the token; revoking a revoked token changes nothing.
    /// </summary>
    /// <param name="now">The moment of revocation.</param>
    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }
}
