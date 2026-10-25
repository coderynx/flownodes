using Registry.Domain.Namespaces;

namespace Registry.Domain.Resources;

/// <summary>
/// The namespaced Kinds the Registry knows. A Namespace is installation-wide, so it is not one of them.
/// </summary>
public sealed class KindCatalog
{
    private readonly Dictionary<string, IResourceKind> _byName;
    private readonly Dictionary<string, IResourceKind> _byPlural;
    private readonly Dictionary<string, IResourceKind> _byRoute;

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
        _byRoute = all.Select(kind => (Route: kind.Plural, Kind: kind))
            .Concat(all.Select(kind => (Route: kind.Name.ToLowerInvariant(), Kind: kind)))
            .DistinctBy(entry => entry.Route)
            .ToDictionary(entry => entry.Route, entry => entry.Kind);
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

    /// <summary>
    /// Finds a Kind by the name a route gives it: its plural, such as <c>connections</c>, or its lowercase singular,
    /// such as <c>connection</c>.
    /// </summary>
    /// <param name="route">The Kind as the route writes it.</param>
    /// <returns>The Kind, or <see langword="null"/> when no namespaced Kind goes by that name.</returns>
    public IResourceKind? FindByRoute(string route) => _byRoute.GetValueOrDefault(route);

    /// <summary>
    /// Finds the Resources that reference one Resource, which therefore cannot be deleted without them (ADR-0007).
    /// </summary>
    /// <param name="namespace">The Namespace of the Resource referenced.</param>
    /// <param name="kind">The Kind of the Resource referenced.</param>
    /// <param name="name">The name of the Resource referenced.</param>
    /// <param name="candidates">The Resources that may reference it.</param>
    /// <returns>The candidates in the same Namespace whose Spec references the Resource.</returns>
    public IEnumerable<Resource> Referrers(
        NamespaceName @namespace,
        string kind,
        ResourceName name,
        IEnumerable<Resource> candidates) =>
        candidates.Where(candidate => candidate.Namespace == @namespace && Find(candidate.Kind)
            ?.References(candidate.Spec)
            .Any(reference => reference.Kind == kind && reference.Name == name.Value) == true);
}
