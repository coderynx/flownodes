using Registry.Domain.Namespaces;

namespace Registry.Domain.Resources;

/// <summary>
/// One value of a Secret, held encrypted apart from the Resource so that neither its Spec nor its Revisions ever carry
/// it.
/// </summary>
public sealed class SecretValue
{
    private SecretValue()
    {
    }

    /// <summary>
    /// Gets the Namespace of the Secret.
    /// </summary>
    public required NamespaceName Namespace { get; init; }
    /// <summary>
    /// Gets the name of the Secret.
    /// </summary>
    public required ResourceName Secret { get; init; }
    /// <summary>
    /// Gets the key the value is stored under.
    /// </summary>
    public required string Key { get; init; }
    /// <summary>
    /// Gets the encrypted value.
    /// </summary>
    public byte[] Encrypted { get; private set; } = [];

    /// <summary>
    /// Holds a value a Secret has just been given.
    /// </summary>
    /// <param name="namespace">The Namespace of the Secret.</param>
    /// <param name="secret">The name of the Secret.</param>
    /// <param name="key">The key the value is stored under.</param>
    /// <param name="encrypted">The encrypted value.</param>
    /// <returns>The held value.</returns>
    public static SecretValue Create(NamespaceName @namespace, ResourceName secret, string key, byte[] encrypted) =>
        new()
        {
            Namespace = @namespace,
            Secret = secret,
            Key = key,
            Encrypted = encrypted
        };

    /// <summary>
    /// Replaces the value.
    /// </summary>
    /// <param name="encrypted">The new encrypted value.</param>
    public void Replace(byte[] encrypted)
    {
        Encrypted = encrypted;
    }
}
