using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Tokens;

namespace Identity.Application.Tokens.CreatePersonalAccessToken;

/// <summary>
/// Creates a Personal Access Token for the caller.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="UserId">The User the token is for; it must be the caller.</param>
/// <param name="Name">The name the User gives the token, 1 to 100 characters.</param>
/// <param name="ExpiresAt">When the token stops working; no expiry when <see langword="null"/>.</param>
public sealed record Command(Guid CallerId, Guid UserId, string Name, DateTimeOffset? ExpiresAt)
    : IRequest<Result<CreatedToken>>;

/// <summary>
/// A newly created Personal Access Token and its secret, which is shown only once.
/// </summary>
/// <param name="Token">The stored token.</param>
/// <param name="Secret">The secret the User presents to exchange the token.</param>
public sealed record CreatedToken(PersonalAccessToken Token, string Secret);

internal sealed class Handler(
    IIdentityDbContext database,
    ICallerChecks checks,
    IPersonalAccessTokenSecrets secrets,
    TimeProvider time) : IRequestHandler<Command, Result<CreatedToken>>
{
    private const int MaxNameLength = 100;

    public async Task<Result<CreatedToken>> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var caller = await checks.RequireActiveAsync(request.CallerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        if (request.UserId != request.CallerId)
        {
            return Errors.ForbiddenOtherUser();
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length is 0 or > MaxNameLength)
        {
            return Errors.InvalidTokenName();
        }

        var now = time.GetUtcNow();
        if (request.ExpiresAt is { } expiresAt && expiresAt <= now)
        {
            return Errors.ExpiryInThePast();
        }

        var secret = secrets.Generate();
        var token = PersonalAccessToken.Create(request.UserId, name, secrets.Hash(secret), now, request.ExpiresAt);
        database.PersonalAccessTokens.Add(token);
        await database.SaveChangesAsync(cancellationToken);

        return Result.Created(new CreatedToken(token, secret));
    }
}
