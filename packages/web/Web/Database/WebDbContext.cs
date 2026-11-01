using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Web.Database;

internal sealed class WebDbContext(DbContextOptions<WebDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public const string Schema = "web";
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
    }
}
