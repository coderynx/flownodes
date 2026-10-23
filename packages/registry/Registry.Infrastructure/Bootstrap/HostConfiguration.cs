using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Registry.Infrastructure.Database;

namespace Registry.Infrastructure.Bootstrap;

/// <summary>
/// Prepares the Registry's infrastructure on the host.
/// </summary>
public static class HostConfiguration
{
    /// <summary>
    /// Maps the shared infrastructure endpoints and, in Development, migrates the Registry's database.
    /// </summary>
    /// <param name="app">The host the Registry is being added to.</param>
    public static void UseInfrastructure(this WebApplication app)
    {
        Core.Infrastructure.Bootstrap.HostConfiguration.UseInfrastructure(app);

        if (app.Environment.IsDevelopment())
        {
            ApplyMigrations(app);
        }
    }

    private static void ApplyMigrations(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RegistryDbContext>();

        dbContext.Database.Migrate();
    }
}
