using Identity.Domain.Namespaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Database.Configurations;

internal sealed class KnownNamespaceConfiguration : IEntityTypeConfiguration<KnownNamespace>
{
    public void Configure(EntityTypeBuilder<KnownNamespace> builder)
    {
        builder.ToTable("known_namespaces");

        builder.HasKey(known => known.Name);
        builder.Property(known => known.Name).HasMaxLength(63);
    }
}
