using Registry.Application.Database;
using Registry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Registry.Infrastructure.Bootstrap;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        Core.Infrastructure.Bootstrap.DependencyInjection.AddOpenTelemetry(builder);
        Core.Infrastructure.Bootstrap.DependencyInjection.AddHealthChecks(builder);
        Core.Infrastructure.Bootstrap.DependencyInjection.AddHttpClientDefaults(builder);

        AddDatabase(builder);
    }

    private static void AddDatabase(IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("registry")
                               ?? throw new InvalidOperationException("Connection string 'registry' not found.");

        builder.Services.AddDbContext<RegistryDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<RegistryDbContext>());
    }
}
