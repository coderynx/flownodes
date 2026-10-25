namespace Registry.Domain.Namespaces;

/// <summary>
/// Whether a Namespace may be deleted, or why not.
/// </summary>
public enum NamespaceDeletion
{
    /// <summary>
    /// The Namespace may be deleted.
    /// </summary>
    Allowed,

    /// <summary>
    /// The Namespace is the <c>default</c> one, which can never be deleted.
    /// </summary>
    Default,

    /// <summary>
    /// Resources would remain in the Namespace.
    /// </summary>
    HoldsResources
}
