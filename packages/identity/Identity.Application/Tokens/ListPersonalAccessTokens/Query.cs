using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Tokens;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Tokens.ListPersonalAccessTokens;

/// <summary>
/// Lists the caller's Personal Access Tokens that are not revoked.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="UserId">The User whose tokens are listed; it must be the caller.</param>
public sealed record Query(Guid CallerId, Guid UserId) : IRequest<Result<IReadOnlyList<PersonalAccessToken>>>;

internal sealed class Handler(IIdentityDbContext database)
    : IRequestHandler<Query, Result<IReadOnlyList<PersonalAccessToken>>>
{
    public async Task<Result<IReadOnlyList<PersonalAccessToken>>> HandleAsync(
        Query request,
        CancellationToken cancellationToken = default)
    {
        var caller = await Callers.RequireActiveAsync(database, request.CallerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        if (request.UserId != request.CallerId)
        {
            return Errors.ForbiddenOtherUser();
        }

        var tokens = await database.PersonalAccessTokens.AsNoTracking()
            .Where(token => token.UserId == request.UserId && token.RevokedAt == null)
            .OrderBy(token => token.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result.Found<IReadOnlyList<PersonalAccessToken>>(tokens);
    }
}
