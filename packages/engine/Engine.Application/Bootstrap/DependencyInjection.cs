using Microsoft.Extensions.Hosting;

namespace Engine.Application.Bootstrap;

public static class DependencyInjection
{
    public static void AddApplication(this IHostApplicationBuilder builder)
    {
        Core.Application.Bootstrap.DependencyInjection.AddCqrs(builder, typeof(DependencyInjection).Assembly);
    }
}
