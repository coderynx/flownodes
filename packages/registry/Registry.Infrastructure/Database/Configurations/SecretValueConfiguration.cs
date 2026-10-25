using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Infrastructure.Database.Configurations;

internal sealed class SecretValueConfiguration : IEntityTypeConfiguration<SecretValue>
{
    public void Configure(EntityTypeBuilder<SecretValue> builder)
    {
        builder.ToTable("secret_values");

        builder.HasKey(value => new { value.Namespace, value.Secret, value.Key });
        builder.Property(value => value.Namespace)
            .HasConversion(name => name.Value, value => NamespaceName.Create(value))
            .HasMaxLength(63);
        builder.Property(value => value.Secret)
            .HasConversion(name => name.Value, value => ResourceName.Create(value))
            .HasMaxLength(63);
        builder.Property(value => value.Key).HasMaxLength(253);
        builder.Property(value => value.Encrypted);
    }
}
