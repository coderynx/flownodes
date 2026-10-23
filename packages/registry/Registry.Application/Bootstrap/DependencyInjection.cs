using Microsoft.Extensions.Hosting;

namespace Registry.Application.Bootstrap;

/// <summary>
/// Adds the Registry's application layer to the host.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Registry's command and query handlers with the shared mediator.
    /// </summary>
    /// <param name="builder">The host the Registry is being added to.</param>
    public static void AddApplication(this IHostApplicationBuilder builder)
    {
        Core.Application.Bootstrap.DependencyInjection.AddCqrs(builder, typeof(DependencyInjection).Assembly);
    }
}
