using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Database.Configurations;

internal sealed class DeletedUserConfiguration : IEntityTypeConfiguration<DeletedUser>
{
    public void Configure(EntityTypeBuilder<DeletedUser> builder)
    {
        builder.ToTable("deleted_users");

        builder.HasKey(deleted => deleted.Id);
        builder.Property(deleted => deleted.Id).ValueGeneratedNever();
    }
}
