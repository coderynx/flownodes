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
