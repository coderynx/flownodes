using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Web.Session;

internal sealed partial class IdentityTokens(
    IOptionsMonitor<OpenIdConnectOptions> options,
    ILogger<IdentityTokens> logger)
{
    private const string InvalidGrant = "invalid_grant";

    public async Task<RefreshOutcome> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var client = options.Get(SessionNames.SignInScheme);

        try
        {
            var configuration = await client.ConfigurationManager!.GetConfigurationAsync(ct);
            using var response = await client.Backchannel.PostAsync(
                configuration.TokenEndpoint,
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["grant_type"] = "refresh_token",
                        ["refresh_token"] = refreshToken,
                        ["client_id"] = client.ClientId!,
                        ["client_secret"] = client.ClientSecret!
                    }),
                ct);

            if (response.IsSuccessStatusCode)
            {
                return new RefreshOutcome(await response.Content.ReadFromJsonAsync<TokenResponse>(ct), false);
            }

            var refusal = await response.Content.ReadFromJsonAsync<TokenRefusal>(ct);

            return new RefreshOutcome(null, refusal?.Error == InvalidGrant);
        }
        catch (Exception exception) when (IsIdentityFault(exception))
        {
            LogRefreshFailed(exception);

            return new RefreshOutcome(null, false);
        }
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct)
    {
        var client = options.Get(SessionNames.SignInScheme);

        try
        {
            var configuration = await client.ConfigurationManager!.GetConfigurationAsync(ct);
            using var response = await client.Backchannel.PostAsync(
                configuration.RevocationEndpoint,
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["token"] = refreshToken,
                        ["token_type_hint"] = SessionNames.RefreshToken,
                        ["client_id"] = client.ClientId!,
                        ["client_secret"] = client.ClientSecret!
                    }),
                ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception exception) when (IsIdentityFault(exception))
        {
            LogRevocationFailed(exception);
        }
    }

    private static bool IsIdentityFault(Exception exception) =>
        exception is HttpRequestException or InvalidOperationException or JsonException or NotSupportedException
            or TaskCanceledException;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refreshing a session's access token failed.")]
    private partial void LogRefreshFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Revoking a session's refresh token failed.")]
    private partial void LogRevocationFailed(Exception exception);

    private sealed record TokenRefusal([property: JsonPropertyName("error")] string? Error);
}

internal sealed record RefreshOutcome(TokenResponse? Tokens, bool IsRejected);

internal sealed record TokenResponse(
    [property: JsonPropertyName(SessionNames.AccessToken)]
    string AccessToken,
    [property: JsonPropertyName(SessionNames.RefreshToken)]
    string? RefreshToken,
    [property: JsonPropertyName(SessionNames.IdentityToken)]
    string? IdentityToken,
    [property: JsonPropertyName("expires_in")]
    int ExpiresIn);
