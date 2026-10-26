using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Tokens.ExchangePersonalAccessToken;

/// <summary>
/// Finds the User a Personal Access Token belongs to, so the token can be exchanged for an access token.
/// </summary>
/// <param name="Secret">The secret as the User presented it.</param>
public sealed record Command(string Secret) : IRequest<Result<User>>;

internal sealed class Handler(IIdentityDbContext database, IPersonalAccessTokenSecrets secrets, TimeProvider time)
    : IRequestHandler<Command, Result<User>>
{
    public async Task<Result<User>> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var hash = secrets.Hash(request.Secret);
        var token = await database.PersonalAccessTokens.SingleOrDefaultAsync(
            candidate => candidate.SecretHash == hash,
            cancellationToken);
        if (token is null || !token.IsActive(time.GetUtcNow()))
        {
            return Errors.InvalidToken();
        }

        var user = await database.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == token.UserId, cancellationToken);

        return user is { IsDisabled: false } ? Result.Found(user) : Errors.InvalidToken();
    }
}
