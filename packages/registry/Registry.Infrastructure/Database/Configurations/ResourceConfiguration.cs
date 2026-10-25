using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Infrastructure.Database.Configurations;

internal sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("resources");

        builder.HasKey(resource => new { resource.Namespace, resource.Kind, resource.Name });
        builder.Property(resource => resource.Namespace)
            .HasConversion(name => name.Value, value => NamespaceName.Create(value))
            .HasMaxLength(63);
        builder.Property(resource => resource.Kind).HasMaxLength(63);
        builder.Property(resource => resource.Name)
            .HasConversion(name => name.Value, value => ResourceName.Create(value))
            .HasMaxLength(63);
        builder.Property(resource => resource.Spec).IsJsonb();
        builder.Property(resource => resource.Status).IsJsonb();

        builder.HasIndex(resource => resource.Uid).IsUnique();
        builder.HasOne<Namespace>()
            .WithMany()
            .HasForeignKey(resource => resource.Namespace)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
