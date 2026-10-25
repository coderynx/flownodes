using System.Security.Cryptography;
using System.Text;
using Core.Infrastructure.Security;
using Registry.Application.Secrets;

namespace Registry.Infrastructure.Secrets;

internal sealed class SecretVault(InstallationKey installationKey) : ISecretVault
{
    private readonly byte[] _fingerprintKey = installationKey.Derive("registry/secret-fingerprint");

    public string Fingerprint(string value) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(_fingerprintKey, Encoding.UTF8.GetBytes(value)));

    public byte[] Encrypt(string value) => installationKey.Encrypt(Encoding.UTF8.GetBytes(value));
}
