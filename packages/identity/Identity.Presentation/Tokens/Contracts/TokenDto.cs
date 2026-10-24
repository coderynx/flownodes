using System.Security.Claims;
using Core.Presentation.Http;
using Identity.Domain.Tokens;

namespace Identity.Presentation.Tokens.Contracts;

internal sealed record TokenDto(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    string? Secret);

internal static class TokenMappings
{
    private const string CallerAlias = "me";

    public static TokenDto ToDto(this PersonalAccessToken token, string? secret = null) =>
        new(token.Id, token.Name, token.CreatedAt, token.ExpiresAt, secret);

    public static Guid ResolveUser(this ClaimsPrincipal caller, string id)
    {
        if (id == CallerAlias)
        {
            return caller.CallerId();
        }

        return Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty;
    }
}
