using Core.Contracts.Identity;
using Core.Contracts.Resources;
using Core.Infrastructure.Security;
using Identity.Application;
using Identity.Application.Database;
using Identity.Application.Security;
using Identity.Domain.Users;
using Identity.Infrastructure.Database;
using Identity.Infrastructure.Messaging;
using Identity.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Infrastructure.Bootstrap;

/// <summary>
/// Adds Identity's infrastructure to the host.
/// </summary>
public static class DependencyInjection
{
    private const string ResourceQueue = "identity-resources";
    private const string UserQueue = "identity-users";
    private const int MinimumPasswordLength = 12;
    private const int MaxFailedAccessAttempts = 5;

    /// <summary>
    /// Registers the shared infrastructure, Identity's Postgres database, the token server and the messaging that
    /// carries Identity's messages and the Registry's Namespace messages.
    /// </summary>
    /// <remarks>
    /// The <c>Flownodes:InstallationKey</c> setting, a base64 256-bit key, encrypts the signing key Identity generates
    /// on first start (ADR-0026).
    /// </remarks>
    /// <param name="builder">The host Identity is being added to.</param>
    /// <exception cref="InvalidOperationException">
    /// The <c>identity</c> connection string is not configured, or the installation key is missing or is not a base64
    /// 256-bit key.
    /// </exception>
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        Core.Infrastructure.Bootstrap.DependencyInjection.AddOpenTelemetry(builder);
        Core.Infrastructure.Bootstrap.DependencyInjection.AddHealthChecks(builder);
        Core.Infrastructure.Bootstrap.DependencyInjection.AddHttpClientDefaults(builder);

        var connectionString = builder.Configuration.GetConnectionString("identity") ??
                               throw new InvalidOperationException("Connection string 'identity' not found.");
        var installationKey = InstallationKey.Parse(builder.Configuration[InstallationKey.Setting]);

        builder.Services.AddSingleton(installationKey);

        AddDatabase(builder, connectionString);
        AddUsers(builder);
        AddDataProtection(builder);
        AddTokenServer(builder);
        AddMessaging(builder, connectionString);

        builder.Services.AddSingleton<SetupTokenService>();
        builder.Services.AddSingleton<ISetupToken>(provider => provider.GetRequiredService<SetupTokenService>());
        builder.Services.AddHostedService<IdentityInitializer>();
    }

    private static void AddDatabase(IHostApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddDbContextWithWolverineIntegration<IdentityDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseOpenIddict();
        });
        builder.Services.AddScoped<IIdentityDbContext, IdentityUnitOfWork>();
        builder.Services.TryAddSingleton(TimeProvider.System);
    }

    private static void AddUsers(IHostApplicationBuilder builder)
    {
        builder.Services.AddIdentityCore<User>(options =>
            {
                options.Password.RequiredLength = MinimumPasswordLength;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = MaxFailedAccessAttempts;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.AllowedUserNameCharacters = string.Empty;
            })
            .AddUserStore<UserStore>();
    }

    private static void AddDataProtection(IHostApplicationBuilder builder)
    {
        builder.Services.AddDataProtection()
            .SetApplicationName("flownodes-identity")
            .PersistKeysToDbContext<IdentityDbContext>();
    }

    private static void AddTokenServer(IHostApplicationBuilder builder)
    {
        var allowInsecure = builder.Configuration.GetValue<bool>("Flownodes:Identity:AllowInsecureTransport");

        builder.Services.AddSingleton<SigningKeyProvider>();
        builder.Services.AddScoped<IUserTokenRevoker, UserTokenRevoker>();
        builder.Services.AddSingleton<IConfigureOptions<OpenIddictServerOptions>, ConfigureServerKeys>();

        builder.Services.AddOpenIddict()
            .AddCore(core => core.UseEntityFrameworkCore().UseDbContext<IdentityDbContext>())
            .AddServer(server =>
            {
                server.SetTokenEndpointUris("connect/token")
                    .SetDeviceAuthorizationEndpointUris("connect/device")
                    .SetEndUserVerificationEndpointUris("connect/verify")
                    .AllowDeviceAuthorizationFlow()
                    .AllowRefreshTokenFlow()
                    .AllowTokenExchangeFlow()
                    .RegisterScopes(Scopes.OfflineAccess)
                    .SetAccessTokenLifetime(TimeSpan.FromMinutes(10))
                    .SetRefreshTokenLifetime(TimeSpan.FromDays(30))
                    .DisableAccessTokenEncryption();

                server.Configure(options => options.SubjectTokenTypes.Add(ExchangeTokenTypes.PersonalAccessToken));

                server.AddEventHandler<OpenIddictServerEvents.ProcessAuthenticationContext>(handler =>
                    handler.UseInlineHandler(context =>
                        {
                            if (context.Transaction.Request is
                                { SubjectTokenType: ExchangeTokenTypes.PersonalAccessToken })
                            {
                                context.ExtractSubjectToken = false;
                                context.ValidateSubjectToken = false;
                                context.RequireSubjectToken = false;
                            }

                            return default;
                        })
                        .SetOrder(OpenIddictServerHandlers.EvaluateValidatedTokens.Descriptor.Order + 500));

                if (builder.Configuration["Flownodes:Identity:Authority"] is { } authority)
                {
                    server.SetIssuer(new Uri(authority));
                }

                var aspNetCore = server.UseAspNetCore()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndUserVerificationEndpointPassthrough();
                if (allowInsecure)
                {
                    aspNetCore.DisableTransportSecurityRequirement();
                }
            });
    }

    private static void AddMessaging(IHostApplicationBuilder builder, string connectionString)
    {
        Core.Infrastructure.Bootstrap.DependencyInjection.AddMessaging<IdentityDbContext>(
            builder,
            connectionString,
            options =>
            {
                options.Discovery.IncludeAssembly(typeof(ResourceMessageHandler).Assembly);

                options.PublishMessage<ResourceApplied>().ToPostgresqlQueue(ResourceQueue);
                options.PublishMessage<ResourceDeleted>().ToPostgresqlQueue(ResourceQueue);
                options.ListenToPostgresqlQueue(ResourceQueue).Sequential();

                options.PublishMessage<UsersResyncRequested>().ToPostgresqlQueue(UserQueue);
                options.ListenToPostgresqlQueue(UserQueue).Sequential();
            });
    }
}
