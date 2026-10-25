using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Flowctl.IntegrationTests;

/// <summary>
/// A stand-in for Identity, the way the Registry's own tests use one: it serves OIDC discovery and JWKS over HTTP for
/// the Registry to validate tokens with, and answers flowctl's device sign-in, Personal Access Token exchange and
/// refresh on the routes Identity uses (ADR-0026).
/// </summary>
public sealed class TestIdentity : IAsyncDisposable
{
    private const string DeviceGrant = "urn:ietf:params:oauth:grant-type:device_code";
    private const string ExchangeGrant = "urn:ietf:params:oauth:grant-type:token-exchange";
    private readonly WebApplication _app;
    private readonly RsaSecurityKey _key;
    private readonly ConcurrentDictionary<string, Guid> _personalAccessTokens = new();
    private readonly ConcurrentDictionary<string, Guid> _refreshTokens = new();
    private readonly ConcurrentDictionary<string, DeviceSignIn> _devices = new();
    private DeviceSignIn? _nextDevice;

    private TestIdentity(WebApplication app, RsaSecurityKey key, Uri address)
    {
        _app = app;
        _key = key;
        Address = address;
    }

    public Uri Address { get; }

    public static async Task<TestIdentity> StartAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var address = new Uri($"http://127.0.0.1:{port}/");
        var key = new RsaSecurityKey(RSA.Create(2048))
        {
            KeyId = Guid.NewGuid().ToString("N")
        };

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(address.ToString());
        builder.Logging.ClearProviders();

        var app = builder.Build();
        app.MapGet(
            "/.well-known/openid-configuration",
            () => Results.Json(
                new Dictionary<string, string>
                {
                    ["issuer"] = address.ToString(),
                    ["jwks_uri"] = new Uri(address, ".well-known/jwks").ToString()
                }));
        app.MapGet(
            "/.well-known/jwks",
            () =>
            {
                var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
                var keys = new
                {
                    keys = new[]
                    {
                        new
                        {
                            kty = jwk.Kty,
                            use = "sig",
                            alg = SecurityAlgorithms.RsaSha256,
                            kid = jwk.Kid,
                            n = jwk.N,
                            e = jwk.E
                        }
                    }
                };

                return Results.Text(JsonSerializer.Serialize(keys), "application/json");
            });

        await app.StartAsync();

        return new TestIdentity(app, key, address);
    }

    public void MapConnect(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/connect/device", () => Device()).AllowAnonymous();
        routes.MapPost("/connect/token", async (HttpRequest request) => Token(await request.ReadFormAsync()))
            .AllowAnonymous();
    }

    public string IssueToken(Guid userId)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Address.ToString(),
            TokenType = "at+jwt",
            Subject = new ClaimsIdentity(
                [new Claim("sub", userId.ToString()), new Claim("jti", Guid.NewGuid().ToString())]),
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public string CreatePersonalAccessToken(Guid userId)
    {
        var secret = $"fn_pat_{Guid.NewGuid():N}";
        _personalAccessTokens[secret] = userId;

        return secret;
    }

    public string CreateRefreshToken(Guid userId)
    {
        var token = $"refresh-{Guid.NewGuid():N}";
        _refreshTokens[token] = userId;

        return token;
    }

    public void SignInNextDevice(Guid? userId, int pendingPolls) =>
        _nextDevice = new DeviceSignIn(userId, pendingPolls);

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private IResult Device()
    {
        var deviceCode = Guid.NewGuid().ToString("N");
        _devices[deviceCode] = _nextDevice ?? new DeviceSignIn(null, 0);

        return Results.Json(
            new Dictionary<string, object>
            {
                ["device_code"] = deviceCode,
                ["user_code"] = "ABCD-EFGH",
                ["verification_uri"] = "http://localhost/connect/verify",
                ["verification_uri_complete"] = "http://localhost/connect/verify?user_code=ABCD-EFGH",
                ["interval"] = 1
            });
    }

    private IResult Token(IFormCollection form)
    {
        switch (form["grant_type"].ToString())
        {
            case ExchangeGrant when _personalAccessTokens.TryGetValue(form["subject_token"].ToString(), out var user):
                return Tokens(user, false);
            case "refresh_token" when _refreshTokens.TryRemove(form["refresh_token"].ToString(), out var user):
                return Tokens(user, true);
            case DeviceGrant when _devices.TryGetValue(form["device_code"].ToString(), out var device):
                if (device.PendingPolls > 0)
                {
                    device.PendingPolls--;

                    return Refuse("authorization_pending");
                }

                return device.UserId is { } signedIn ? Tokens(signedIn, true) : Refuse("access_denied");
            default:
                return Refuse("invalid_grant");
        }
    }

    private IResult Tokens(Guid userId, bool withRefresh)
    {
        var tokens = new Dictionary<string, object>
        {
            ["access_token"] = IssueToken(userId),
            ["token_type"] = "Bearer",
            ["expires_in"] = 600
        };
        if (withRefresh)
        {
            tokens["refresh_token"] = CreateRefreshToken(userId);
        }

        return Results.Json(tokens);
    }

    private static IResult Refuse(string error) =>
        Results.Json(new Dictionary<string, string> { ["error"] = error }, statusCode: StatusCodes.Status400BadRequest);

    private sealed class DeviceSignIn(Guid? userId, int pendingPolls)
    {
        public Guid? UserId { get; } = userId;
        public int PendingPolls { get; set; } = pendingPolls;
    }
}
