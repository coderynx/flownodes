namespace Registry.Domain.Namespaces;

/// <summary>
/// What the Registry keeps of a deleted Namespace: the sequence number of its deletion message, so a Namespace
/// re-created with the same name continues its predecessor's sequence (ADR-0014).
/// </summary>
public sealed class DeletedNamespace
{
    private DeletedNamespace()
    {
    }

    /// <summary>
    /// Gets the name the deleted Namespace had.
    /// </summary>
    public required NamespaceName Name { get; init; }
    /// <summary>
    /// Gets the sequence number of the message that deleted the Namespace.
    /// </summary>
    public required long Seq { get; init; }

    internal static DeletedNamespace Create(NamespaceName name, long seq) =>
        new()
        {
            Name = name,
            Seq = seq
        };
}
