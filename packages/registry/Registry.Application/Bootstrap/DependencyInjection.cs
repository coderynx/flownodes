using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Registry.Application.Resources;
using Registry.Domain.Resources;

namespace Registry.Application.Bootstrap;

/// <summary>
/// Adds the Registry's application layer to the host.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Registry's command and query handlers with the shared mediator, the catalogue of namespaced Kinds
    /// built from every registered <see cref="IResourceKind"/>, and the Revision retention.
    /// </summary>
    /// <param name="builder">The host the Registry is being added to.</param>
    public static void AddApplication(this IHostApplicationBuilder builder)
    {
        Core.Application.Bootstrap.DependencyInjection.AddCqrs(builder, typeof(DependencyInjection).Assembly);

        builder.Services.TryAddSingleton(services => new KindCatalog(services.GetServices<IResourceKind>()));
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddOptions<RevisionRetention>().BindConfiguration(RevisionRetention.Section);
    }
}
