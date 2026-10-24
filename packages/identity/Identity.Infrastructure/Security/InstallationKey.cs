using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Security;

internal sealed class InstallationKey
{
    public const string Setting = "Flownodes:InstallationKey";
    private const int KeyBytes = 32;
    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private readonly byte[] _key;

    private InstallationKey(byte[] key)
    {
        _key = key;
    }

    public static InstallationKey Parse(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            throw new InvalidOperationException($"The setting '{Setting}' is required: a base64 256-bit key.");
        }

        var buffer = new byte[base64.Length];
        if (!Convert.TryFromBase64String(base64, buffer, out var written) || written != KeyBytes)
        {
            throw new InvalidOperationException($"The setting '{Setting}' must be a base64 256-bit key.");
        }

        return new InstallationKey(buffer[..written]);
    }

    public byte[] Derive(string purpose) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, _key, KeyBytes, info: Encoding.UTF8.GetBytes(purpose));

    public byte[] Encrypt(byte[] plain)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagBytes];

        using var aes = new AesGcm(_key, TagBytes);
        aes.Encrypt(nonce, plain, cipher, tag);

        return [.. nonce, .. tag, .. cipher];
    }

    public byte[] Decrypt(byte[] sealedBytes)
    {
        var nonce = sealedBytes[..NonceBytes];
        var tag = sealedBytes[NonceBytes..(NonceBytes + TagBytes)];
        var cipher = sealedBytes[(NonceBytes + TagBytes)..];
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, TagBytes);
        aes.Decrypt(nonce, cipher, tag, plain);

        return plain;
    }
}
