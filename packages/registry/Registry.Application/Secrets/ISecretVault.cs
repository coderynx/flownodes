namespace Registry.Application.Secrets;

/// <summary>
/// Protects the values of Secrets with the installation key (ADR-0014).
/// </summary>
public interface ISecretVault
{
    /// <summary>
    /// Computes the fingerprint the Registry keeps in a Secret's Spec: equal for equal values under one installation
    /// key, and useless to anyone without it.
    /// </summary>
    /// <param name="value">The value of a Secret.</param>
    /// <returns>The fingerprint, as text.</returns>
    string Fingerprint(string value);

    /// <summary>
    /// Encrypts a value for storage.
    /// </summary>
    /// <param name="value">The value of a Secret.</param>
    /// <returns>The encrypted value.</returns>
    byte[] Encrypt(string value);
}
