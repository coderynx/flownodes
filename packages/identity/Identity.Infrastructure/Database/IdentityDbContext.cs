using Identity.Domain.Namespaces;
using Identity.Domain.Tokens;
using Identity.Domain.Users;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Database;

internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RoleGrant> RoleGrants => Set<RoleGrant>();
    public DbSet<DeletedUser> DeletedUsers => Set<DeletedUser>();
    public DbSet<PersonalAccessToken> PersonalAccessTokens => Set<PersonalAccessToken>();
    public DbSet<KnownNamespace> KnownNamespaces => Set<KnownNamespace>();
    public DbSet<SigningKey> SigningKeys => Set<SigningKey>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        modelBuilder.UseOpenIddict();
    }
}
