using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Engine.IntegrationTests;

/// <summary>
/// A stand-in for Identity's token issuer: it serves OIDC discovery and JWKS over HTTP and signs access tokens whose
/// subject is a User id, the way the Registry meets the real one (ADR-0026).
/// </summary>
public sealed class TestIdentity : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly RsaSecurityKey _key;

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

    public string IssueToken(Guid userId, string? name)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Address.ToString(),
            TokenType = "at+jwt",
            Subject = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim("sub", userId.ToString()),
                new System.Security.Claims.Claim("jti", Guid.NewGuid().ToString()),
                .. name is null ? [] : new[] { new System.Security.Claims.Claim("name", name) }
            ]),
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
