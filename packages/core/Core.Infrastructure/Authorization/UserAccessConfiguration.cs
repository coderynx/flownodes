using Core.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Authorization;

/// <summary>
/// Maps a module's replica of Users and Role Grants. A module applies it in its database context so every module
/// stores the replica the same way.
/// </summary>
public sealed class UserAccessConfiguration : IEntityTypeConfiguration<UserAccess>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserAccess> builder)
    {
        builder.ToTable("user_access");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.Username).HasMaxLength(64);
        builder.Property(user => user.LastSeq).IsConcurrencyToken();

        builder.Ignore(user => user.IsActive);
        builder.Navigation(user => user.Grants).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsMany(
            user => user.Grants,
            grants =>
            {
                grants.ToTable("user_access_grants");
                grants.WithOwner().HasForeignKey("UserId");
                grants.Property<int>("Id");
                grants.HasKey("Id");
                grants.Property(grant => grant.Namespace).HasMaxLength(63);
                grants.Property(grant => grant.Role).HasConversion<string>().HasMaxLength(16);
            });
    }
}
