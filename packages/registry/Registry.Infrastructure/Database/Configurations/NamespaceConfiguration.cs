using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Namespaces;

namespace Registry.Infrastructure.Database.Configurations;

internal sealed class NamespaceConfiguration : IEntityTypeConfiguration<Namespace>
{
    public void Configure(EntityTypeBuilder<Namespace> builder)
    {
        builder.ToTable("namespaces");

        builder.HasKey(@namespace => @namespace.Name);
        builder.Property(@namespace => @namespace.Name)
            .HasConversion(name => name.Value, value => NamespaceName.Create(value))
            .HasMaxLength(63);

        builder.HasIndex(@namespace => @namespace.Uid).IsUnique();
    }
}
