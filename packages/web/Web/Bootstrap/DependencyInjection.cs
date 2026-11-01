using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Web.Database;
using Web.Session;

namespace Web.Bootstrap;

/// <summary>
/// Adds the web UI to the host.
/// </summary>
public static class DependencyInjection
{
    private const string ConnectionStringName = "web";
    private const string AuthoritySetting = "Flownodes:Identity:Authority";
    private const string AllowInsecureTransportSetting = "Flownodes:Identity:AllowInsecureTransport";
    private const string InstallationKeySetting = "Flownodes:InstallationKey";
    private const string ClientId = "web";
    private const string ClientSecretPurpose = "web/client-secret";
    private const string SessionTable = "sessions";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    /// <summary>
    /// Registers what serving the web UI and signing a User in through the backend-for-frontend need: sessions and Data
    /// Protection keys in Postgres, and the OIDC client of Identity (ADR-0030).
    /// </summary>
    /// <param name="builder">The host the web UI is being added to.</param>
    /// <exception cref="InvalidOperationException">
    /// The <c>web</c> connection string, the <c>Flownodes:Identity:Authority</c> setting or the
    /// <c>Flownodes:InstallationKey</c> setting is not configured.
    /// </exception>
    public static void AddWeb(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRouting(options =>
            options.SetParameterPolicy<ClientRouteConstraint>(ClientRouteConstraint.Name));
        builder.Services.AddHttpForwarder();

        var connectionString = builder.Configuration.GetConnectionString(ConnectionStringName) ??
                               throw new InvalidOperationException(
                                   $"Connection string '{ConnectionStringName}' not found.");

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddDbContext<WebDbContext>(options => UseDatabase(options, connectionString));
        builder.Services.AddHostedService<WebInitializer>();

        AddSession(builder, connectionString);
    }

    private static void AddSession(IHostApplicationBuilder builder, string connectionString)
    {
        var dataProtection = CreateDataProtection(connectionString);
        var authority = Required(builder, AuthoritySetting);
        var clientSecret = ClientSecret(Required(builder, InstallationKeySetting));

        builder.Services.AddDistributedPostgresCache(options =>
        {
            options.ConnectionString = connectionString;
            options.SchemaName = WebDbContext.Schema;
            options.TableName = SessionTable;
            options.CreateIfNotExists = true;
            options.UseWAL = true;
        });
        builder.Services.AddSingleton(services => new SessionStore(
            services.GetRequiredService<IDistributedCache>(),
            dataProtection.CreateProtector(nameof(SessionStore))));
        builder.Services.AddSingleton<IdentityTokens>();
        builder.Services.AddScoped<SessionRefresh>();
        builder.Services.AddOptions<CookieAuthenticationOptions>(SessionNames.SessionScheme)
            .Configure<SessionStore>((options, store) => options.SessionStore = store);

        builder.Services.AddAuthentication()
            .AddCookie(
                SessionNames.SessionScheme,
                options =>
                {
                    options.DataProtectionProvider = dataProtection;
                    options.EventsType = typeof(SessionRefresh);
                    options.ExpireTimeSpan = SessionLifetime;
                    options.Cookie.Name = SessionNames.Cookie;
                    options.Cookie.Path = "/";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                })
            .AddOpenIdConnect(
                SessionNames.SignInScheme,
                options =>
                {
                    options.DataProtectionProvider = dataProtection;
                    options.SignInScheme = SessionNames.SessionScheme;
                    options.Authority = authority;
                    options.RequireHttpsMetadata = !builder.Configuration.GetValue<bool>(AllowInsecureTransportSetting);
                    options.ClientId = ClientId;
                    options.ClientSecret = clientSecret;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.ResponseMode = OpenIdConnectResponseMode.Query;
                    options.UsePkce = true;
                    options.SaveTokens = true;
                    options.MapInboundClaims = false;
                    options.CallbackPath = "/bff/signin-callback";
                    options.SignedOutCallbackPath = "/bff/signout-callback";
                    options.Scope.Remove("profile");
                    options.Scope.Add(OpenIdConnectScope.OfflineAccess);
                    options.Events.OnRemoteFailure = SignInRestart.RestartOnceAsync;
                    options.Events.OnTicketReceived = context =>
                    {
                        context.Properties!.IsPersistent = true;

                        return Task.CompletedTask;
                    };
                });
        builder.Services.AddAuthorization();
    }

    private static void UseDatabase(DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, WebDbContext.Schema));

    // HACK: a host has one Data Protection stack and Identity configures it with its own key table, so Web keeps its
    // keys in its own schema through a container of its own.
    private static IDataProtectionProvider CreateDataProtection(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<WebDbContext>(options => UseDatabase(options, connectionString));
        services.AddDataProtection().SetApplicationName("flownodes-web").PersistKeysToDbContext<WebDbContext>();

        return services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
    }

    private static string Required(IHostApplicationBuilder builder, string setting) =>
        builder.Configuration[setting] ?? throw new InvalidOperationException($"Setting '{setting}' not found.");

    private static string ClientSecret(string installationKey) =>
        Convert.ToBase64String(
            HKDF.DeriveKey(
                HashAlgorithmName.SHA256,
                Convert.FromBase64String(installationKey),
                outputLength: 32,
                info: Encoding.UTF8.GetBytes(ClientSecretPurpose)));
}
