using System.Collections.Immutable;
using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application;
using Identity.Application.SignIn.FindSignInUser;
using Identity.Application.Tokens.ExchangePersonalAccessToken;
using Identity.Domain.Users;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Presentation.Connect;

internal static class TokenEndpoint
{
    public static void MapTokenEndpoint(this WebApplication app)
    {
        app.MapPost("/connect/token", HandleAsync).WithName("Token").DisableAntiforgery().AllowAnonymous();
    }

    public static ClaimsPrincipal SignInPrincipal(User user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity("Flownodes", Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Name, user.Username.Value);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);
        principal.SetDestinations(claim =>
            claim.Type is Claims.Subject or Claims.Name ? [Destinations.AccessToken] : []);

        return principal;
    }

    public static IResult Reject(string error, string description) =>
        Results.Forbid(
            new AuthenticationProperties(
                new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
                }),
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);

    private static async Task<IResult> HandleAsync(HttpContext http, ISender sender, CancellationToken ct)
    {
        var request = http.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsDeviceCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            return await ReissueAsync(http, sender, ct);
        }

        if (request.IsTokenExchangeGrantType())
        {
            return await ExchangeAsync(request, sender, ct);
        }

        return Reject(Errors.UnsupportedGrantType, "This grant type is not supported.");
    }

    private static async Task<IResult> ReissueAsync(HttpContext http, ISender sender, CancellationToken ct)
    {
        var authenticated = await http.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var subject = authenticated.Principal?.GetClaim(Claims.Subject);
        if (!Guid.TryParse(subject, out var userId))
        {
            return Reject(Errors.InvalidGrant, "The token is no longer valid.");
        }

        var found = await sender.SendAsync(new Query(userId), ct);

        return found.Match(
            user => Results.SignIn(
                SignInPrincipal(user, authenticated.Principal!.GetScopes()),
                properties: null,
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme),
            _ => Reject(Errors.InvalidGrant, "The User may no longer sign in."));
    }

    private static async Task<IResult> ExchangeAsync(OpenIddictRequest request, ISender sender, CancellationToken ct)
    {
        if (request.SubjectTokenType != ExchangeTokenTypes.PersonalAccessToken ||
            string.IsNullOrEmpty(request.SubjectToken))
        {
            return Reject(Errors.InvalidRequest, "Exchange a Personal Access Token with its subject token type.");
        }

        var exchanged = await sender.SendAsync(new Command(request.SubjectToken), ct);

        return exchanged.Match(
            user => Results.SignIn(
                SignInPrincipal(user, ImmutableArray<string>.Empty),
                properties: null,
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme),
            _ => Reject(Errors.InvalidGrant, "The Personal Access Token is unknown, revoked or expired."));
    }
}
