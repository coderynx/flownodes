namespace Identity.Domain.Namespaces;

/// <summary>
/// A Namespace as Identity knows it from the Registry's messages: the only Namespaces a role can be granted in.
/// </summary>
public sealed class KnownNamespace
{
    /// <summary>
    /// The Kind a Namespace has in the Registry's messages.
    /// </summary>
    public const string Kind = "Namespace";

    /// <summary>
    /// The name of the Namespace every installation starts with, which Identity knows without any message.
    /// </summary>
    public const string Default = "default";

    private KnownNamespace()
    {
    }

    /// <summary>
    /// Gets the name of the Namespace.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// Gets the identifier of the incarnation of the Namespace last heard of.
    /// </summary>
    public Guid Uid { get; private set; }
    /// <summary>
    /// Gets the sequence number of the last message applied about the Namespace.
    /// </summary>
    public long Seq { get; private set; }

    /// <summary>
    /// Records a Namespace the Registry announced.
    /// </summary>
    /// <param name="name">The name of the Namespace.</param>
    /// <param name="uid">The identifier of this incarnation of the Namespace.</param>
    /// <param name="seq">The sequence number of the message that announced it.</param>
    /// <returns>The known Namespace.</returns>
    public static KnownNamespace Create(string name, Guid uid, long seq) =>
        new()
        {
            Name = name,
            Uid = uid,
            Seq = seq
        };

    /// <summary>
    /// Applies a later message about the Namespace, unless the message is not newer than the last one applied.
    /// </summary>
    /// <param name="uid">The identifier of the incarnation the message is about.</param>
    /// <param name="seq">The sequence number of the message.</param>
    /// <returns><see langword="true"/> when the message was newer and applied.</returns>
    public bool Apply(Guid uid, long seq)
    {
        if (uid == Uid && seq <= Seq)
        {
            return false;
        }

        Uid = uid;
        Seq = seq;

        return true;
    }
}
