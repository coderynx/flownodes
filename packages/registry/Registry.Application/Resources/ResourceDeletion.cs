using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Registry.Application.Database;
using Registry.Application.Namespaces;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

/// <summary>
/// One Resource a delete removed, or would remove on a dry run.
/// </summary>
/// <param name="Kind">The Kind of the Resource.</param>
/// <param name="Namespace">The Namespace of the Resource, or <see langword="null"/> for a Namespace.</param>
/// <param name="Name">The name of the Resource.</param>
public sealed record DeletedResource(string Kind, string? Namespace, string Name);

internal sealed class ResourceDeletion(IApplicationDbContext database, KindCatalog catalog, TimeProvider time, int keep)
{
    public async Task<Result<IReadOnlyList<DeletedResource>>> DeleteAsync(
        Guid callerId,
        ManifestReading reading,
        int documentCount,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var access = await database.AuthoriseChangesAsync(callerId, reading, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        if (documentCount == 0)
        {
            return Errors.EmptyManifest();
        }

        if (reading.Problems.Count > 0)
        {
            return new ManifestRejected(reading.Problems);
        }

        var state = await ManifestState.LoadAsync(database, catalog, reading.Entries, cancellationToken);
        if (state.IsFailure)
        {
            return state.Error;
        }

        var refused = Refuse(reading.Entries, state.Value);
        if (refused is not null)
        {
            return refused;
        }

        if (!dryRun)
        {
            var author = new RevisionAuthor(callerId, access.Value!.Username);
            await RemoveAsync(reading.Entries, state.Value, author, cancellationToken);
        }

        return Result.Found<IReadOnlyList<DeletedResource>>(
        [
            .. reading.Entries.Select(entry => new DeletedResource(entry.Kind, entry.NamespaceOrNull, entry.Name.Value))
        ]);
    }

    private Error? Refuse(IReadOnlyList<ManifestEntry> entries, ManifestState state)
    {
        var missing = entries.FirstOrDefault(entry =>
            entry.IsNamespace ? state.FindNamespace(entry.Namespace) is null : state.FindResource(entry) is null);
        if (missing is not null)
        {
            return missing.IsNamespace
                ? Errors.NamespaceNotFound(missing.Name.Value)
                : Errors.NotFound(missing.Kind, missing.Name.Value);
        }

        var remaining = state.Resources.Where(resource => !entries.Any(entry => entry.Is(resource))).ToList();
        var problems = entries.Select(entry => Problem(entry, state, remaining)).OfType<ManifestProblem>().ToList();

        return problems.Count > 0 ? new DeletionRefused(problems) : null;
    }

    private ManifestProblem? Problem(ManifestEntry entry, ManifestState state, List<Resource> remaining)
    {
        if (entry.IsNamespace)
        {
            var deletion = state.FindNamespace(entry.Namespace)!.CheckDeletion(
                remaining.Exists(resource => resource.Namespace == entry.Namespace));

            return deletion switch
            {
                NamespaceDeletion.Default => Refusal(entry, "The default Namespace cannot be deleted."),
                NamespaceDeletion.HoldsResources => Refusal(
                    entry,
                    "The Namespace still holds Resources. Delete them first."),
                _ => null
            };
        }

        var referrers = catalog.Referrers(entry.Namespace, entry.Kind, entry.Name, remaining)
            .Select(resource => $"{resource.Kind} '{resource.Name}'")
            .ToList();

        return referrers.Count == 0
            ? null
            : Refusal(entry, $"Still referenced by {string.Join(", ", referrers)}. Delete or change those first.");
    }

    private static ManifestProblem Refusal(ManifestEntry entry, string message) =>
        new(entry.Document, entry.Kind, entry.Name.Value, "metadata.name", message);

    private async Task RemoveAsync(
        IReadOnlyList<ManifestEntry> entries,
        ManifestState state,
        RevisionAuthor author,
        CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();

        foreach (var entry in entries.Where(entry => !entry.IsNamespace))
        {
            var resource = state.FindResource(entry)!;
            resource.Delete(state.FindNamespace(entry.Namespace)!.NextSeq());
            database.Resources.Remove(resource);
            await database.PublishAsync(resource.ToDeleted());
            await database.RecordAsync(Revision.Tombstone(resource, author, now), keep, cancellationToken);
        }

        foreach (var entry in entries.Where(entry => entry.IsNamespace))
        {
            var @namespace = state.FindNamespace(entry.Namespace)!;
            @namespace.Delete();
            database.Namespaces.Remove(@namespace);
            await database.PublishAsync(@namespace.ToDeleted());
            await database.RecordAsync(Revision.Tombstone(@namespace, author, now), keep, cancellationToken);
        }

        await database.SaveChangesAsync(cancellationToken);
    }
}
