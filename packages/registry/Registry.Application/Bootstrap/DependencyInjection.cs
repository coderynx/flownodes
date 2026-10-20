using Microsoft.Extensions.Hosting;

namespace Registry.Application.Bootstrap;

public static class DependencyInjection
{
    public static void AddApplication(this IHostApplicationBuilder builder)
    {
        Core.Application.Bootstrap.DependencyInjection.AddCqrs(builder, typeof(DependencyInjection).Assembly);
    }
}
