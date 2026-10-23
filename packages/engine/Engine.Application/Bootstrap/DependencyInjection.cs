using Microsoft.Extensions.Hosting;

namespace Engine.Application.Bootstrap;

/// <summary>
/// Adds the Engine's application layer to the host.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Engine's command and query handlers with the shared mediator.
    /// </summary>
    /// <param name="builder">The host the Engine is being added to.</param>
    public static void AddApplication(this IHostApplicationBuilder builder)
    {
        Core.Application.Bootstrap.DependencyInjection.AddCqrs(builder, typeof(DependencyInjection).Assembly);
    }
}
