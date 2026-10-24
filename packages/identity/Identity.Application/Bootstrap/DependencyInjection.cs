using Microsoft.Extensions.Hosting;

namespace Identity.Application.Bootstrap;

/// <summary>
/// Adds Identity's application layer to the host.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Identity's command and query handlers with the shared mediator.
    /// </summary>
    /// <param name="builder">The host Identity is being added to.</param>
    public static void AddApplication(this IHostApplicationBuilder builder)
    {
        Core.Application.Bootstrap.DependencyInjection.AddCqrs(builder, typeof(DependencyInjection).Assembly);
    }
}
