using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core.Presentation.Bootstrap;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the shared presentation services (OpenAPI document generator behind the Scalar API reference).
    /// Safe to call from every module: the registration runs once per host.
    /// </summary>
    public static void AddPresentation(this IHostApplicationBuilder builder)
    {
        if (builder.Services.Any(service => service.ServiceType == typeof(PresentationMarker)))
        {
            return;
        }

        builder.Services.AddSingleton<PresentationMarker>();
        builder.Services.AddOpenApi();
    }

    private sealed class PresentationMarker;
}
