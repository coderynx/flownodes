using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Web.Session;

internal sealed class SessionRefresh(IdentityTokens tokens, TimeProvider time) : CookieAuthenticationEvents
{
    private static readonly TimeSpan Margin = TimeSpan.FromMinutes(1);

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var properties = context.Properties;
        var now = time.GetUtcNow();
        if (!IsCloseToExpiry(properties, now) || properties.GetTokenValue(SessionNames.RefreshToken) is not
                { } refreshToken)
        {
            return;
        }

        var outcome = await tokens.RefreshAsync(refreshToken, context.HttpContext.RequestAborted);
        if (outcome.IsRejected)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(SessionNames.SessionScheme);

            return;
        }

        if (outcome.Tokens is not { } refreshed)
        {
            return;
        }

        properties.UpdateTokenValue(SessionNames.AccessToken, refreshed.AccessToken);
        properties.UpdateTokenValue(SessionNames.RefreshToken, refreshed.RefreshToken ?? refreshToken);
        properties.UpdateTokenValue(
            SessionNames.ExpiresAt,
            now.AddSeconds(refreshed.ExpiresIn).ToString("o", CultureInfo.InvariantCulture));
        if (refreshed.IdentityToken is not null)
        {
            properties.UpdateTokenValue(SessionNames.IdentityToken, refreshed.IdentityToken);
        }

        context.ShouldRenew = true;
    }

    private static bool IsCloseToExpiry(AuthenticationProperties properties, DateTimeOffset now) =>
        DateTimeOffset.TryParse(
            properties.GetTokenValue(SessionNames.ExpiresAt),
            CultureInfo.InvariantCulture,
            out var expiresAt) && expiresAt - now <= Margin;
}
