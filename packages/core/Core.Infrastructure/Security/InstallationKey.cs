using System.Security.Cryptography;
using System.Text;

namespace Core.Infrastructure.Security;

/// <summary>
/// The one key a deployment supplies, identically on every Silo, from which everything Flownodes keeps encrypted at
/// rest is protected (ADR-0014).
/// </summary>
public sealed class InstallationKey
{
    /// <summary>
    /// The configuration setting that holds the key.
    /// </summary>
    public const string Setting = "Flownodes:InstallationKey";

    private const int KeyBytes = 32;
    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private readonly byte[] _key;

    private InstallationKey(byte[] key)
    {
        _key = key;
    }

    /// <summary>
    /// Reads the key from its configured text.
    /// </summary>
    /// <param name="base64">The value of <see cref="Setting"/>.</param>
    /// <returns>The key.</returns>
    /// <exception cref="InvalidOperationException">The value is missing or is not a base64 256-bit key.</exception>
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

    /// <summary>
    /// Derives a key for one purpose, so no two uses share key material.
    /// </summary>
    /// <param name="purpose">What the derived key is for.</param>
    /// <returns>A 256-bit key.</returns>
    public byte[] Derive(string purpose) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, _key, KeyBytes, info: Encoding.UTF8.GetBytes(purpose));

    /// <summary>
    /// Encrypts bytes with AES-256-GCM under a fresh nonce.
    /// </summary>
    /// <param name="plain">The bytes to protect.</param>
    /// <returns>The nonce, the tag and the cipher text, in that order.</returns>
    public byte[] Encrypt(byte[] plain)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagBytes];

        using var aes = new AesGcm(_key, TagBytes);
        aes.Encrypt(nonce, plain, cipher, tag);

        return [.. nonce, .. tag, .. cipher];
    }

    /// <summary>
    /// Decrypts bytes <see cref="Encrypt"/> produced.
    /// </summary>
    /// <param name="sealedBytes">The nonce, the tag and the cipher text.</param>
    /// <returns>The original bytes.</returns>
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
