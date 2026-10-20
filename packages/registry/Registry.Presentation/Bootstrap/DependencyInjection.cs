using Microsoft.Extensions.Hosting;

namespace Registry.Presentation.Bootstrap;

public static class DependencyInjection
{
    public static void AddPresentation(this IHostApplicationBuilder builder)
    {
        Core.Presentation.Bootstrap.DependencyInjection.AddPresentation(builder);
    }
}
