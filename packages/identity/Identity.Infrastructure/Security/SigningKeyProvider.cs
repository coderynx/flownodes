using System.Security.Cryptography;
using Identity.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;

namespace Identity.Infrastructure.Security;

internal sealed class SigningKeyProvider
{
    private RsaSecurityKey? _signingKey;
    public RsaSecurityKey SigningKey =>
        _signingKey ?? throw new InvalidOperationException("The signing key is not loaded yet.");

    public async Task LoadAsync(IdentityDbContext database, InstallationKey installationKey, CancellationToken ct)
    {
        var stored = await database.SigningKeys.AsNoTracking()
            .SingleOrDefaultAsync(key => key.Id == Database.SigningKey.CurrentId, ct);

        if (stored is null)
        {
            using var generated = RSA.Create(3072);
            var created = new Database.SigningKey
            {
                Id = Database.SigningKey.CurrentId,
                EncryptedPrivateKey = installationKey.Encrypt(generated.ExportPkcs8PrivateKey())
            };

            await database.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT INTO signing_keys ("Id", "EncryptedPrivateKey")
                 VALUES ({created.Id}, {created.EncryptedPrivateKey})
                 ON CONFLICT ("Id") DO NOTHING
                 """,
                ct);

            stored = await database.SigningKeys.AsNoTracking()
                .SingleAsync(key => key.Id == Database.SigningKey.CurrentId, ct);
        }

        var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(installationKey.Decrypt(stored.EncryptedPrivateKey), out _);
        _signingKey = new RsaSecurityKey(rsa)
        {
            KeyId = "flownodes"
        };
    }
}

internal sealed class ConfigureServerKeys(SigningKeyProvider provider, InstallationKey installationKey)
    : IConfigureOptions<OpenIddictServerOptions>
{
    public void Configure(OpenIddictServerOptions options)
    {
        options.SigningCredentials.Add(new SigningCredentials(provider.SigningKey, SecurityAlgorithms.RsaSha256));
        options.EncryptionCredentials.Add(
            new EncryptingCredentials(
                new SymmetricSecurityKey(installationKey.Derive("identity/openiddict-encryption")),
                SecurityAlgorithms.Aes256KW,
                SecurityAlgorithms.Aes128CbcHmacSha256));
    }
}
