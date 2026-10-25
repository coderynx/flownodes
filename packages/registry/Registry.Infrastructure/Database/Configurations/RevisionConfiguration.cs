using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Infrastructure.Database.Configurations;

internal sealed class RevisionConfiguration : IEntityTypeConfiguration<Revision>
{
    public void Configure(EntityTypeBuilder<Revision> builder)
    {
        builder.ToTable("revisions");

        builder.HasKey(revision => revision.Id);
        builder.Property(revision => revision.Id).UseIdentityAlwaysColumn();
        builder.Property(revision => revision.Namespace)
            .HasConversion(name => name.Value, value => NamespaceName.Create(value))
            .HasMaxLength(63);
        builder.Property(revision => revision.Kind).HasMaxLength(63);
        builder.Property(revision => revision.Name)
            .HasConversion(name => name.Value, value => ResourceName.Create(value))
            .HasMaxLength(63);
        builder.Property(revision => revision.Spec).IsJsonb();
        builder.ComplexProperty(
            revision => revision.Author,
            author =>
            {
                author.Property(value => value.UserId).HasColumnName("author_user_id");
                author.Property(value => value.Username).HasColumnName("author_username").HasMaxLength(64);
            });

        builder.HasIndex(revision => new { revision.Namespace, revision.Kind, revision.Name, revision.Id });
    }
}
