using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Tokens.RevokePersonalAccessToken;

/// <summary>
/// Revokes one of the caller's Personal Access Tokens.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="UserId">The User who owns the token; it must be the caller.</param>
/// <param name="TokenId">The id of the token.</param>
public sealed record Command(Guid CallerId, Guid UserId, Guid TokenId) : IRequest<Result>;

internal sealed class Handler(IIdentityDbContext database, TimeProvider time) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var caller = await CallerChecks.RequireActiveAsync(database, request.CallerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        if (request.UserId != request.CallerId)
        {
            return Errors.ForbiddenOtherUser();
        }

        var token = await database.PersonalAccessTokens.SingleOrDefaultAsync(
            candidate => candidate.Id == request.TokenId && candidate.UserId == request.UserId,
            cancellationToken);
        if (token is null)
        {
            return Errors.TokenNotFound();
        }

        token.Revoke(time.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);

        return Result.Deleted();
    }
}
