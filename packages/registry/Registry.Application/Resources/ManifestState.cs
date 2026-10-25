using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal sealed class ManifestState
{
    private readonly List<Namespace> _namespaces;
    private readonly List<Resource> _resources;

    private ManifestState(List<Namespace> namespaces, List<Resource> resources)
    {
        _namespaces = namespaces;
        _resources = resources;
    }

    public IReadOnlyList<Resource> Resources => _resources;

    public static async Task<Result<ManifestState>> LoadAsync(
        IApplicationDbContext database,
        IReadOnlyList<ManifestEntry> entries,
        CancellationToken cancellationToken)
    {
        var names = ResourceAccess.LockOrder(entries);
        await database.LockNamespacesAsync(names, cancellationToken);

        var namespaceNames = names.Select(NamespaceName.Create).ToList();
        var namespaces = await database.Namespaces.Where(@namespace => namespaceNames.Contains(@namespace.Name))
            .ToListAsync(cancellationToken);
        var missing = entries.FirstOrDefault(entry =>
            !entry.IsNamespace && namespaces.TrueForAll(@namespace => @namespace.Name != entry.Namespace));
        if (missing is not null)
        {
            return Errors.NamespaceNotFound(missing.Namespace.Value);
        }

        var resources = await database.Resources.Where(resource => namespaceNames.Contains(resource.Namespace))
            .ToListAsync(cancellationToken);

        return Result.Found(new ManifestState(namespaces, resources));
    }

    public Namespace? FindNamespace(NamespaceName name) =>
        _namespaces.Find(@namespace => @namespace.Name == name);

    public Resource? FindResource(ManifestEntry entry) => _resources.Find(entry.Is);

    public IEnumerable<ManifestProblem> UnresolvedReferences(IReadOnlyList<ManifestEntry> entries) =>
        from entry in entries
        where !entry.IsNamespace
        from reference in entry.Rules!.References(entry.Spec)
        where !entries.Any(other => Names(other, entry.Namespace, reference)) && !_resources.Exists(resource =>
            resource.Namespace == entry.Namespace && resource.Kind == reference.Kind &&
            resource.Name.Value == reference.Name)
        select new ManifestProblem(
            entry.Document,
            entry.Kind,
            entry.Name.Value,
            reference.Path,
            $"References {reference.Kind} '{reference.Name}', which does not exist in Namespace " +
            $"'{entry.Namespace}'.");

    public Error? FirstStalePrecondition(IReadOnlyList<ManifestEntry> entries)
    {
        foreach (var entry in entries.Where(entry => entry.Generation is not null))
        {
            var current = entry.IsNamespace
                ? FindNamespace(entry.Namespace)?.Generation
                : FindResource(entry)?.Generation;
            if (current != entry.Generation)
            {
                return Errors.GenerationConflict(entry.Kind, entry.Name.Value, entry.Generation!.Value, current);
            }
        }

        return null;
    }

    private static bool Names(ManifestEntry entry, NamespaceName @namespace, SpecReference reference) =>
        !entry.IsNamespace && entry.Namespace == @namespace && entry.Kind == reference.Kind &&
        entry.Name.Value == reference.Name;
}
