using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flowctl;

internal sealed class Tokens(CliContext context, HttpClient http)
{
    private const string ClientId = "flowctl";
    private const string DeviceGrant = "urn:ietf:params:oauth:grant-type:device_code";
    private const string ExchangeGrant = "urn:ietf:params:oauth:grant-type:token-exchange";
    private const string PersonalAccessTokenType = "urn:flownodes:params:oauth:token-type:personal-access-token";
    private static readonly TimeSpan Margin = TimeSpan.FromSeconds(30);

    public async Task<Profile?> SignInWithDeviceAsync(Uri server, CancellationToken cancellationToken)
    {
        var response = await http.PostAsync(
            new Uri(server, "connect/device"),
            Form(("client_id", ClientId), ("scope", "offline_access")),
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await context.Error.WriteLineAsync(
                $"error: {server} refused the device sign-in ({(int)response.StatusCode}).");

            return null;
        }

        var device = (await response.Content.ReadFromJsonAsync<DeviceAuthorization>(cancellationToken))!;
        await context.Output.WriteLineAsync(
            $"Open {device.VerificationUriComplete ?? device.VerificationUri} and confirm the code {device.UserCode}.");

        var interval = TimeSpan.FromSeconds(Math.Max(device.Interval ?? 5, 1));
        while (true)
        {
            await context.Delay(interval, cancellationToken);

            var poll = await RequestAsync(
                server,
                Form(("grant_type", DeviceGrant), ("device_code", device.DeviceCode), ("client_id", ClientId)),
                cancellationToken);
            switch (poll.Error)
            {
                case null:
                    return Signed(server, poll, null);
                case "authorization_pending":
                    continue;
                case "slow_down":
                    interval += TimeSpan.FromSeconds(5);
                    continue;
                default:
                    await context.Error.WriteLineAsync($"error: sign-in failed: {poll.ErrorDescription ?? poll.Error}");

                    return null;
            }
        }
    }

    public async Task<Profile?> SignInWithPersonalAccessTokenAsync(
        Uri server,
        string personalAccessToken,
        CancellationToken cancellationToken)
    {
        var exchanged = await ExchangeAsync(server, personalAccessToken, cancellationToken);
        if (exchanged.Error is not null)
        {
            await context.Error.WriteLineAsync(
                $"error: the Personal Access Token was refused: {exchanged.ErrorDescription ?? exchanged.Error}");

            return null;
        }

        return Signed(server, exchanged, personalAccessToken);
    }

    public async Task<string?> AccessTokenAsync(Uri server, Profile profile, CancellationToken cancellationToken)
    {
        if (context.Variable("FLOWCTL_TOKEN") is { Length: > 0 } fromEnvironment)
        {
            return (await ExchangeAsync(server, fromEnvironment, cancellationToken)).AccessToken;
        }

        if (profile.AccessToken is not null && profile.ExpiresAt > context.Time.GetUtcNow())
        {
            return profile.AccessToken;
        }

        var renewed = await RenewAsync(server, profile, cancellationToken);
        if (renewed is not { Error: null })
        {
            return null;
        }

        var signed = Signed(server, renewed, profile.PersonalAccessToken);
        signed.RefreshToken ??= profile.RefreshToken;
        signed.Save(context.ProfilePath);

        return signed.AccessToken;
    }

    private async Task<TokenResponse?> RenewAsync(Uri server, Profile profile, CancellationToken cancellationToken)
    {
        if (profile.RefreshToken is not null)
        {
            return await RequestAsync(
                server,
                Form(("grant_type", "refresh_token"), ("refresh_token", profile.RefreshToken), ("client_id", ClientId)),
                cancellationToken);
        }

        return profile.PersonalAccessToken is null
            ? null
            : await ExchangeAsync(server, profile.PersonalAccessToken, cancellationToken);
    }

    private Task<TokenResponse> ExchangeAsync(
        Uri server,
        string personalAccessToken,
        CancellationToken cancellationToken) =>
        RequestAsync(
            server,
            Form(
                ("grant_type", ExchangeGrant),
                ("client_id", ClientId),
                ("subject_token", personalAccessToken),
                ("subject_token_type", PersonalAccessTokenType)),
            cancellationToken);

    private async Task<TokenResponse> RequestAsync(Uri server, HttpContent form, CancellationToken cancellationToken)
    {
        var response = await http.PostAsync(new Uri(server, "connect/token"), form, cancellationToken);
        try
        {
            return await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken) ??
                   new TokenResponse { Error = $"HTTP {(int)response.StatusCode}" };
        }
        catch (JsonException)
        {
            return new TokenResponse { Error = $"HTTP {(int)response.StatusCode}" };
        }
    }

    private Profile Signed(Uri server, TokenResponse tokens, string? personalAccessToken) =>
        new()
        {
            Server = server.ToString(),
            AccessToken = tokens.AccessToken,
            ExpiresAt = context.Time.GetUtcNow() + TimeSpan.FromSeconds(tokens.ExpiresIn ?? 0) - Margin,
            RefreshToken = tokens.RefreshToken,
            PersonalAccessToken = personalAccessToken
        };

    private static FormUrlEncodedContent Form(params (string Key, string Value)[] fields) =>
        new(fields.Select(field => new KeyValuePair<string, string>(field.Key, field.Value)));

    private sealed record DeviceAuthorization(
        [property: JsonPropertyName("device_code")]
        string DeviceCode,
        [property: JsonPropertyName("user_code")]
        string UserCode,
        [property: JsonPropertyName("verification_uri")]
        string VerificationUri,
        [property: JsonPropertyName("verification_uri_complete")]
        string? VerificationUriComplete,
        [property: JsonPropertyName("interval")]
        int? Interval);

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; init; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }
        [JsonPropertyName("expires_in")] public int? ExpiresIn { get; init; }
        [JsonPropertyName("error")] public string? Error { get; init; }
        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; init; }
    }
}
