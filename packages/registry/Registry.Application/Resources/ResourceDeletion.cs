using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Registry.Application.Database;
using Registry.Application.Namespaces;
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
        var access = await database.AuthoriseChangesAsync(callerId, reading.Entries, cancellationToken);
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

        var state = await ManifestState.LoadAsync(database, reading.Entries, cancellationToken);
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
            await RemoveAsync(
                reading.Entries,
                state.Value,
                new RevisionAuthor(callerId, access.Value!.Username),
                cancellationToken);
        }

        return Result.Found<IReadOnlyList<DeletedResource>>(
        [
            .. reading.Entries.Select(entry => new DeletedResource(entry.Kind, entry.NamespaceOrNull, entry.Name.Value))
        ]);
    }

    private Error? Refuse(IReadOnlyList<ManifestEntry> entries, ManifestState state)
    {
        foreach (var entry in entries)
        {
            if (entry.IsNamespace ? state.FindNamespace(entry.Namespace) is null : state.FindResource(entry) is null)
            {
                return entry.IsNamespace
                    ? Errors.NamespaceNotFound(entry.Name.Value)
                    : Errors.NotFound(entry.Kind, entry.Name.Value);
            }
        }

        var remaining = state.Resources.Where(resource => !entries.Any(entry => entry.Is(resource))).ToList();

        foreach (var entry in entries)
        {
            if (entry.IsNamespace)
            {
                if (entry.Namespace.IsDefault)
                {
                    return Errors.DefaultNamespace();
                }

                if (remaining.Exists(resource => resource.Namespace == entry.Namespace))
                {
                    return Errors.NamespaceHoldsResources(entry.Name.Value);
                }

                continue;
            }

            var referrers = remaining
                .Where(resource => resource.Namespace == entry.Namespace && References(resource, entry))
                .Select(resource => $"{resource.Kind} '{resource.Name}'")
                .ToList();
            if (referrers.Count > 0)
            {
                return Errors.Referenced(entry.Kind, entry.Name.Value, referrers);
            }
        }

        return null;
    }

    private bool References(Resource resource, ManifestEntry entry) =>
        catalog.Find(resource.Kind)
            ?.References(resource.Spec)
            .Any(reference => reference.Kind == entry.Kind && reference.Name == entry.Name.Value) == true;

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
