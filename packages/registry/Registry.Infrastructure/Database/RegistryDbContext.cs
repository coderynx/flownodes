using Core.Domain.Authorization;
using Core.Infrastructure.Authorization;
using Microsoft.EntityFrameworkCore;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Infrastructure.Database;

internal sealed class RegistryDbContext(DbContextOptions<RegistryDbContext> options) : DbContext(options)
{
    public DbSet<Namespace> Namespaces => Set<Namespace>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Revision> Revisions => Set<Revision>();
    public DbSet<UserAccess> UserAccess => Set<UserAccess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RegistryDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new UserAccessConfiguration());
    }
}
