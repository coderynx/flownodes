using System.Text.Json;

namespace Registry.Domain.Namespaces;

/// <summary>
/// A Namespace as the Registry stores it: the unit of tenant isolation. It is installation-wide, so it is its own
/// Namespace for the purpose of the sequence number its messages carry.
/// </summary>
public sealed class Namespace
{
    /// <summary>
    /// The Kind a Namespace has in messages.
    /// </summary>
    public const string Kind = "Namespace";

    /// <summary>
    /// The plural a route and <c>flowctl</c> give the Kind.
    /// </summary>
    public const string Plural = "namespaces";

    private Namespace()
    {
    }

    /// <summary>
    /// Gets the name that identifies the Namespace.
    /// </summary>
    public required NamespaceName Name { get; init; }
    /// <summary>
    /// Gets the identifier of this incarnation of the Namespace.
    /// </summary>
    public required Guid Uid { get; init; }
    /// <summary>
    /// Gets the Generation of the Namespace's Spec.
    /// </summary>
    public long Generation { get; private set; }
    /// <summary>
    /// Gets the Spec every Namespace has: an empty object, since a Namespace has nothing to configure yet.
    /// </summary>
    public static JsonElement Spec { get; } = JsonDocument.Parse("{}").RootElement.Clone();
    /// <summary>
    /// Gets the Namespace's gap-free sequence number: that of the last message published about the Namespace or any
    /// Resource in it (ADR-0014).
    /// </summary>
    public long Seq { get; private set; }

    /// <summary>
    /// Creates a Namespace at Generation 1 and sequence number 1, the number its creation message carries.
    /// </summary>
    /// <param name="name">The name of the new Namespace.</param>
    /// <returns>The new Namespace.</returns>
    public static Namespace Create(NamespaceName name) =>
        new()
        {
            Name = name,
            Uid = Guid.NewGuid(),
            Generation = 1,
            Seq = 1
        };

    /// <summary>
    /// Advances the sequence number for a message about a Resource in the Namespace.
    /// </summary>
    /// <returns>The number the message carries.</returns>
    public long NextSeq() => ++Seq;

    /// <summary>
    /// Advances the sequence number to the one the deletion message carries.
    /// </summary>
    /// <exception cref="InvalidOperationException">The Namespace is the <c>default</c> Namespace.</exception>
    public void Delete()
    {
        if (Name.IsDefault)
        {
            throw new InvalidOperationException("The default Namespace cannot be deleted.");
        }

        Seq++;
    }
}
