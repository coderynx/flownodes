using Registry.Application.Database;
using Microsoft.EntityFrameworkCore;

namespace Registry.Infrastructure.Database;

public class RegistryDbContext(DbContextOptions<RegistryDbContext> options) : DbContext(options), IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RegistryDbContext).Assembly);
    }
}
