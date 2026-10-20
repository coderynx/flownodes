using Registry.Infrastructure.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Registry.Infrastructure.Bootstrap;

public static class HostConfiguration
{
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
