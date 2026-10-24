namespace Core.Domain.Authorization;

/// <summary>
/// One Role Grant in a Namespace, as a module's replica of Identity's records holds it.
/// </summary>
public sealed class NamespaceGrant
{
    private NamespaceGrant()
    {
        Namespace = string.Empty;
    }

    internal NamespaceGrant(string @namespace, NamespaceRole role)
    {
        Namespace = @namespace;
        Role = role;
    }

    /// <summary>
    /// Gets the name of the Namespace the role applies to.
    /// </summary>
    public string Namespace { get; private set; }
    /// <summary>
    /// Gets the role held in that Namespace.
    /// </summary>
    public NamespaceRole Role { get; private set; }
}
