using Microsoft.Extensions.Hosting;

namespace Registry.Presentation.Bootstrap;

/// <summary>
/// Adds the Registry's HTTP surface to the host.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the presentation services the Registry's endpoints need, and the authentication of callers by
    /// Identity's access tokens.
    /// </summary>
    /// <param name="builder">The host the Registry is being added to.</param>
    public static void AddPresentation(this IHostApplicationBuilder builder)
    {
        Core.Presentation.Bootstrap.DependencyInjection.AddPresentation(builder);
        Core.Presentation.Bootstrap.DependencyInjection.AddTokenValidation(builder);
    }
}
