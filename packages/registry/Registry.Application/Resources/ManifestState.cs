using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal sealed class ManifestState
{
    private readonly KindCatalog _catalog;
    private readonly List<Namespace> _namespaces;
    private readonly List<DeletedNamespace> _deletedNamespaces;
    private readonly List<Resource> _resources;

    private ManifestState(
        KindCatalog catalog,
        List<Namespace> namespaces,
        List<DeletedNamespace> deletedNamespaces,
        List<Resource> resources)
    {
        _catalog = catalog;
        _namespaces = namespaces;
        _deletedNamespaces = deletedNamespaces;
        _resources = resources;
    }

    public IReadOnlyList<Resource> Resources => _resources;

    public static async Task<Result<ManifestState>> LoadAsync(
        IApplicationDbContext database,
        KindCatalog catalog,
        IReadOnlyList<ManifestEntry> entries,
        CancellationToken cancellationToken)
    {
        var names = entries.Select(entry => entry.Namespace.Value).Distinct().Order(StringComparer.Ordinal).ToList();
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

        var deletedNamespaces = await database.DeletedNamespaces
            .Where(@namespace => namespaceNames.Contains(@namespace.Name))
            .ToListAsync(cancellationToken);
        var resources = await database.Resources.Where(resource => namespaceNames.Contains(resource.Namespace))
            .ToListAsync(cancellationToken);

        return Result.Found(new ManifestState(catalog, namespaces, deletedNamespaces, resources));
    }

    public Namespace? FindNamespace(NamespaceName name) =>
        _namespaces.Find(@namespace => @namespace.Name == name);

    public DeletedNamespace? FindDeletedNamespace(NamespaceName name) =>
        _deletedNamespaces.Find(@namespace => @namespace.Name == name);

    public Resource? FindResource(ManifestEntry entry) => _resources.Find(entry.Is);

    public IEnumerable<ManifestProblem> Problems(IReadOnlyList<ManifestEntry> entries)
    {
        var problems = new List<ManifestProblem>();

        foreach (var group in entries.Where(entry => !entry.IsNamespace).GroupBy(entry => entry.Namespace))
        {
            var stored = _resources.Where(resource => resource.Namespace == group.Key).ToList();
            var before = new NamespaceSnapshot(group.Key, stored.Select(Snapshot));
            var untouched = stored.Where(resource => !group.Any(entry => entry.Is(resource))).ToList();
            var after = new NamespaceSnapshot(
                group.Key,
                untouched.Select(Snapshot)
                    .Concat(group.Select(entry => new SnapshotResource(entry.Kind, entry.Name, entry.Spec))));

            foreach (var entry in group)
            {
                problems.AddRange(EntryProblems(entry, after));
            }

            foreach (var resource in untouched)
            {
                problems.AddRange(NewProblems(resource, before, after));
            }
        }

        return problems;
    }

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

    private static SnapshotResource Snapshot(Resource resource) => new(resource.Kind, resource.Name, resource.Spec);

    private static IEnumerable<ManifestProblem> EntryProblems(ManifestEntry entry, NamespaceSnapshot after)
    {
        var errors = entry.Rules!.Validate(entry.Spec, after).ToList();
        if (errors.Count > 0)
        {
            return errors.Select(error => Problem(entry, error.Path, error.Message));
        }

        return entry.Rules.References(entry.Spec)
            .Where(reference => after.Find(reference.Kind, reference.Name) is null)
            .Select(reference => Problem(
                entry,
                reference.Path,
                $"References {reference.Kind} '{reference.Name}', which does not exist in Namespace " +
                $"'{entry.Namespace}'."));
    }

    private IEnumerable<ManifestProblem> NewProblems(
        Resource resource,
        NamespaceSnapshot before,
        NamespaceSnapshot after)
    {
        if (_catalog.Find(resource.Kind) is not { } rules)
        {
            return [];
        }

        var existing = rules.Validate(resource.Spec, before).ToHashSet();

        return rules.Validate(resource.Spec, after)
            .Where(error => !existing.Contains(error))
            .Select(error => new ManifestProblem(
                null,
                resource.Kind,
                resource.Name.Value,
                error.Path,
                $"{error.Message} This Resource is not in the Manifest, but the Manifest changes its meaning."));
    }

    private static ManifestProblem Problem(ManifestEntry entry, string path, string message) =>
        new(entry.Document, entry.Kind, entry.Name.Value, path, message);
}
