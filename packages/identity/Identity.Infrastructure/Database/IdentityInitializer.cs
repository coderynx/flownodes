using Core.Infrastructure.Security;
using Identity.Domain.Users;
using Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Infrastructure.Database;

internal sealed partial class IdentityInitializer(
    IServiceScopeFactory scopeFactory,
    InstallationKey installationKey,
    SigningKeyProvider signingKeys,
    SetupTokenService setupToken,
    IConfiguration configuration,
    ILogger<IdentityInitializer> logger) : IHostedLifecycleService
{
    public const string ClientId = "flowctl";
    private const string WebClientId = "web";
    private const string WebClientSecretPurpose = "web/client-secret";
    private const string WebSignInCallback = "bff/signin-callback";
    private const string WebSignOutCallback = "bff/signout-callback";

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync(cancellationToken);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        await signingKeys.LoadAsync(database, installationKey, cancellationToken);

        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        await SeedClientAsync(applications, cancellationToken);
        await SeedWebClientAsync(applications, cancellationToken);

        if (!await database.Set<User>().AnyAsync(cancellationToken))
        {
            LogSetupToken(setupToken.Generate());
        }
    }

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task SeedClientAsync(IOpenIddictApplicationManager applications, CancellationToken ct)
    {
        if (await applications.FindByClientIdAsync(ClientId, ct) is not null)
        {
            return;
        }

        await applications.CreateAsync(
            new OpenIddictApplicationDescriptor
            {
                ClientId = ClientId,
                ClientType = ClientTypes.Public,
                DisplayName = "flowctl",
                Permissions =
                {
                    Permissions.Endpoints.DeviceAuthorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.DeviceCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.GrantTypes.TokenExchange,
                    Permissions.Prefixes.Scope + Scopes.OfflineAccess
                }
            },
            ct);
    }

    private async Task SeedWebClientAsync(IOpenIddictApplicationManager applications, CancellationToken ct)
    {
        if (configuration["Flownodes:Identity:Authority"] is not { } authority)
        {
            return;
        }

        var origin = new Uri(authority);
        var signInCallback = new Uri(origin, WebSignInCallback);
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = WebClientId,
            ClientSecret = Convert.ToBase64String(installationKey.Derive(WebClientSecretPurpose)),
            ClientType = ClientTypes.Confidential,
            DisplayName = "Flownodes web UI",
            RedirectUris = { signInCallback },
            PostLogoutRedirectUris = { new Uri(origin, WebSignOutCallback) },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.EndSession,
                Permissions.Endpoints.Revocation,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Prefixes.Scope + Scopes.OfflineAccess
            }
        };

        var existing = await applications.FindByClientIdAsync(WebClientId, ct);
        if (existing is null)
        {
            await applications.CreateAsync(descriptor, ct);
        }
        else if (!await applications.ValidateRedirectUriAsync(existing, signInCallback.AbsoluteUri, ct))
        {
            await applications.UpdateAsync(existing, descriptor, ct);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No User exists yet. Setup token: {Token}")]
    private partial void LogSetupToken(string token);
}
