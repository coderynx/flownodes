using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Database.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Username)
            .HasConversion(username => username.Value, value => Username.Create(value))
            .HasColumnType("citext")
            .HasMaxLength(64);
        builder.HasIndex(user => user.Username).IsUnique();
        builder.Property(user => user.DisplayName).HasMaxLength(100);

        builder.HasMany(user => user.RoleGrants).WithOne().HasForeignKey(grant => grant.UserId);
        builder.Navigation(user => user.RoleGrants).HasField("_roleGrants");
    }
}

internal sealed class RoleGrantConfiguration : IEntityTypeConfiguration<RoleGrant>
{
    public void Configure(EntityTypeBuilder<RoleGrant> builder)
    {
        builder.ToTable("role_grants");

        builder.HasKey(grant => new { grant.UserId, grant.Namespace, grant.Role });
        builder.Property(grant => grant.Namespace).HasMaxLength(63);
        builder.Property(grant => grant.Role).HasConversion<string>().HasMaxLength(16);
    }
}
