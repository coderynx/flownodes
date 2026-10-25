namespace Registry.Domain.Resources;

/// <summary>
/// The namespaced Kinds the Registry knows. A Namespace is installation-wide, so it is not one of them.
/// </summary>
public sealed class KindCatalog
{
    private readonly Dictionary<string, IResourceKind> _byName;
    private readonly Dictionary<string, IResourceKind> _byPlural;

    /// <summary>
    /// Initializes a new instance of the <see cref="KindCatalog"/> class.
    /// </summary>
    /// <param name="kinds">Every namespaced Kind.</param>
    /// <exception cref="ArgumentException">Two Kinds share a name or a plural.</exception>
    public KindCatalog(IEnumerable<IResourceKind> kinds)
    {
        var all = kinds.ToList();
        var duplicate = all.GroupBy(kind => kind.Name)
            .Concat(all.GroupBy(kind => kind.Plural))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Kind '{duplicate.Key}' is registered twice.", nameof(kinds));
        }

        _byName = all.ToDictionary(kind => kind.Name);
        _byPlural = all.ToDictionary(kind => kind.Plural);
    }

    /// <summary>
    /// Finds a Kind by the name a Manifest gives it.
    /// </summary>
    /// <param name="name">The Kind, PascalCase and singular.</param>
    /// <returns>The Kind, or <see langword="null"/> when no namespaced Kind has that name.</returns>
    public IResourceKind? Find(string name) => _byName.GetValueOrDefault(name);

    /// <summary>
    /// Finds a Kind by the plural a route gives it.
    /// </summary>
    /// <param name="plural">The Kind, lowercase and plural.</param>
    /// <returns>The Kind, or <see langword="null"/> when no namespaced Kind has that plural.</returns>
    public IResourceKind? FindByPlural(string plural) => _byPlural.GetValueOrDefault(plural);
}
