using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Namespaces;

namespace Registry.Infrastructure.Database.Configurations;

internal sealed class DeletedNamespaceConfiguration : IEntityTypeConfiguration<DeletedNamespace>
{
    public void Configure(EntityTypeBuilder<DeletedNamespace> builder)
    {
        builder.ToTable("deleted_namespaces");

        builder.HasKey(@namespace => @namespace.Name);
        builder.Property(@namespace => @namespace.Name)
            .HasConversion(name => name.Value, value => NamespaceName.Create(value))
            .HasMaxLength(63);
    }
}
