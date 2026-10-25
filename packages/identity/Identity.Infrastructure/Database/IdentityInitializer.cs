using Identity.Domain.Users;
using Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
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
    ILogger<IdentityInitializer> logger) : IHostedLifecycleService
{
    public const string ClientId = "flowctl";

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
        await SeedClientAsync(
            scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>(),
            cancellationToken);

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

    [LoggerMessage(Level = LogLevel.Warning, Message = "No User exists yet. Setup token: {Token}")]
    private partial void LogSetupToken(string token);
}
