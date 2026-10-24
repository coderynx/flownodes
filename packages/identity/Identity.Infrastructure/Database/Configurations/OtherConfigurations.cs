using Identity.Domain.Namespaces;
using Identity.Domain.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Database.Configurations;

internal sealed class PersonalAccessTokenConfiguration : IEntityTypeConfiguration<PersonalAccessToken>
{
    public void Configure(EntityTypeBuilder<PersonalAccessToken> builder)
    {
        builder.ToTable("personal_access_tokens");

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Name).HasMaxLength(100);
        builder.Property(token => token.SecretHash).HasMaxLength(64);
        builder.HasIndex(token => token.SecretHash).IsUnique();
        builder.HasIndex(token => token.UserId);
    }
}

internal sealed class KnownNamespaceConfiguration : IEntityTypeConfiguration<KnownNamespace>
{
    public void Configure(EntityTypeBuilder<KnownNamespace> builder)
    {
        builder.ToTable("known_namespaces");

        builder.HasKey(known => known.Name);
        builder.Property(known => known.Name).HasMaxLength(63);
    }
}

internal sealed class SigningKeyConfiguration : IEntityTypeConfiguration<SigningKey>
{
    public void Configure(EntityTypeBuilder<SigningKey> builder)
    {
        builder.ToTable("signing_keys");

        builder.HasKey(key => key.Id);
        builder.Property(key => key.Id).HasMaxLength(32);
    }
}
