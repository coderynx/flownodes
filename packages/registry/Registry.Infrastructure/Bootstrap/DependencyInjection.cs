using Core.Application.Authorization;
using Core.Contracts.Identity;
using Core.Contracts.Resources;
using Core.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Registry.Application.Database;
using Registry.Application.Secrets;
using Registry.Infrastructure.Database;
using Registry.Infrastructure.Messaging;
using Registry.Infrastructure.Secrets;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

namespace Registry.Infrastructure.Bootstrap;

/// <summary>
/// Adds the Registry's infrastructure to the host.
/// </summary>
public static class DependencyInjection
{
    private const string ResourceQueue = "resources";
    private const string IdentityQueue = "registry-identity";
    private const string EngineQueue = "registry-engine";

    /// <summary>
    /// Registers the shared infrastructure, the Registry's Postgres database, the messaging that carries its
    /// Resource messages to the other modules and the messaging that brings it Identity's Role Grants and the
    /// Engine's resync requests and Status reports. On start, it asks Identity to publish every User again.
    /// </summary>
    /// <remarks>
    /// The <c>Flownodes:InstallationKey</c> setting, a base64 256-bit key, encrypts the values of Secrets.
    /// </remarks>
    /// <param name="builder">The host the Registry is being added to.</param>
    /// <exception cref="InvalidOperationException">
    /// The <c>registry</c> connection string is not configured, or the installation key is missing or is not a base64
    /// 256-bit key.
    /// </exception>
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        Core.Infrastructure.Bootstrap.DependencyInjection.AddOpenTelemetry(builder);
        Core.Infrastructure.Bootstrap.DependencyInjection.AddHealthChecks(builder);
        Core.Infrastructure.Bootstrap.DependencyInjection.AddHttpClientDefaults(builder);

        var connectionString = builder.Configuration.GetConnectionString("registry") ??
                               throw new InvalidOperationException("Connection string 'registry' not found.");

        builder.Services.AddSingleton<ISecretVault>(
            new SecretVault(InstallationKey.Parse(builder.Configuration[InstallationKey.Setting])));

        AddDatabase(builder, connectionString);
        AddMessaging(builder, connectionString);

        builder.Services.AddHostedService<RegistryInitializer>();
    }

    private static void AddDatabase(IHostApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddDbContextWithWolverineIntegration<RegistryDbContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddScoped<IApplicationDbContext, RegistryUnitOfWork>();
    }

    private static void AddMessaging(IHostApplicationBuilder builder, string connectionString)
    {
        Core.Infrastructure.Bootstrap.DependencyInjection.AddMessaging<RegistryDbContext>(
            builder,
            connectionString,
            options =>
            {
                options.PublishMessage<ResourceApplied>().ToPostgresqlQueue(ResourceQueue);
                options.PublishMessage<ResourceDeleted>().ToPostgresqlQueue(ResourceQueue);

                options.Discovery.IncludeAssembly(typeof(IdentityMessageHandler).Assembly);

                options.PublishMessage<NamespacesResyncRequested>().ToPostgresqlQueue(EngineQueue);
                options.PublishMessage<StatusReported>().ToPostgresqlQueue(EngineQueue);
                options.ListenToPostgresqlQueue(EngineQueue).Sequential();

                options.PublishMessage<RoleGranted>().ToPostgresqlQueue(IdentityQueue);
                options.PublishMessage<RoleRevoked>().ToPostgresqlQueue(IdentityQueue);
                options.PublishMessage<AdministratorGranted>().ToPostgresqlQueue(IdentityQueue);
                options.PublishMessage<AdministratorRevoked>().ToPostgresqlQueue(IdentityQueue);
                options.PublishMessage<UserDisabled>().ToPostgresqlQueue(IdentityQueue);
                options.PublishMessage<UserEnabled>().ToPostgresqlQueue(IdentityQueue);
                options.PublishMessage<UserDeleted>().ToPostgresqlQueue(IdentityQueue);
                options.PublishMessage<UserRenamed>().ToPostgresqlQueue(IdentityQueue);
                options.PublishMessage<UserAccessPublished>().ToPostgresqlQueue(IdentityQueue);
                options.ListenToPostgresqlQueue(IdentityQueue).Sequential();
            });

        builder.Services.TryAddSingleton<IUserAccessResync, WolverineUserAccessResync>();
        builder.Services.AddHostedService<ResyncUsersOnStart>();
    }
}
