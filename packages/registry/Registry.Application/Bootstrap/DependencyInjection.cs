using Core.Application.Integrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Registry.Application.Database;
using Registry.Application.Namespaces;
using Registry.Application.Resources;
using Registry.Application.Secrets;
using Registry.Domain.Resources;

namespace Registry.Application.Bootstrap;

/// <summary>
/// Adds the Registry's application layer to the host.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Registry's command and query handlers with the shared mediator, the catalogue of namespaced Kinds
    /// built from the Secret, Connection, Device, Asset Type and Asset Kinds and every other registered
    /// <see cref="IResourceKind"/>, the Revision retention, and the services that authorise, store Secret values, log
    /// Revisions and delete Resources.
    /// </summary>
    /// <param name="builder">The host the Registry is being added to.</param>
    public static void AddApplication(this IHostApplicationBuilder builder)
    {
        Core.Application.Bootstrap.DependencyInjection.AddCqrs(builder, typeof(DependencyInjection).Assembly);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IResourceKind, SecretKind>());
        builder.Services.TryAddSingleton<IIntegrationSchemas, NoIntegrations>();
        builder.Services.TryAddSingleton<Surfaces>();
        builder.Services.TryAddSingleton<AssetSources>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IResourceKind, ConnectionKind>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IResourceKind, DeviceKind>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IResourceKind, AssetTypeKind>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IResourceKind, AssetKind>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IResourceKind, FlowKind>());
        builder.Services.TryAddSingleton(services => new KindCatalog(services.GetServices<IResourceKind>()));
        builder.Services.TryAddSingleton(TimeProvider.System);
        Core.Application.Bootstrap.DependencyInjection.AddAccessChecks<IApplicationDbContext>(builder);
        builder.Services.TryAddScoped<IResourceAccess, ResourceAccess>();
        builder.Services.TryAddScoped<IRevisionLog, RevisionLog>();
        builder.Services.TryAddScoped<ISecretStorage, SecretStorage>();
        builder.Services.TryAddScoped<ResourceDeletion>();
        builder.Services.TryAddScoped<INamespaceCreation, NamespaceCreation>();
        builder.Services.AddOptions<RevisionRetention>().BindConfiguration(RevisionRetention.Section);
    }
}
