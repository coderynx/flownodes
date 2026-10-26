using Identity.Application.Database;
using Identity.Application.Security;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application;

internal interface IUserTokens
{
    Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken);
}

internal sealed class UserTokens(IIdentityDbContext database, IUserTokenRevoker revoker, TimeProvider time)
    : IUserTokens
{
    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        await revoker.RevokeAllAsync(userId, cancellationToken);

        var now = time.GetUtcNow();
        var tokens = await database.PersonalAccessTokens.Where(token => token.UserId == userId)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.Revoke(now);
        }
    }
}
