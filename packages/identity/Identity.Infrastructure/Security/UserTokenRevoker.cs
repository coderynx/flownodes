using Identity.Application.Security;
using OpenIddict.Abstractions;

namespace Identity.Infrastructure.Security;

internal sealed class UserTokenRevoker(IOpenIddictTokenManager tokens, IOpenIddictAuthorizationManager authorizations)
    : IUserTokenRevoker
{
    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var subject = userId.ToString();

        await foreach (var token in tokens.FindBySubjectAsync(subject, cancellationToken))
        {
            await tokens.TryRevokeAsync(token, cancellationToken);
        }

        await foreach (var authorization in authorizations.FindBySubjectAsync(subject, cancellationToken))
        {
            await authorizations.TryRevokeAsync(authorization, cancellationToken);
        }
    }
}
