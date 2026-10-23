using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;

namespace Registry.Infrastructure.Database;

internal sealed class RegistryDbContext(DbContextOptions<RegistryDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RegistryDbContext).Assembly);
    }
}
