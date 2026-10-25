using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Microsoft.Extensions.Options;
using Registry.Application.Database;
using Registry.Application.Namespaces;
using Registry.Application.Secrets;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Application.Resources.ApplyManifest;

/// <summary>
/// Applies a Manifest: every Resource in it gets the whole Spec written, or, when anything in it is invalid, none does
/// (ADR-0007). A namespaced Resource needs the Engineer role in its Namespace; a Namespace needs an Administrator.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Documents">The Resources of the Manifest, each as written, <c>status</c> ignored.</param>
/// <param name="Namespace">
/// The Namespace the caller is working in, which a Resource without <c>metadata.namespace</c> goes to and a Resource
/// with another one is rejected for; <see langword="null"/> for <c>default</c> and no check.
/// </param>
/// <param name="DryRun">Runs every check and reports what would change, without changing anything.</param>
public sealed record Command(Guid CallerId, IReadOnlyList<JsonElement> Documents, string? Namespace, bool DryRun)
    : IRequest<Result<IReadOnlyList<AppliedResource>>>;

/// <summary>
/// What applying a Manifest did, or would do on a dry run, to one of its Resources.
/// </summary>
/// <param name="Kind">The Kind of the Resource.</param>
/// <param name="Namespace">The Namespace of the Resource, or <see langword="null"/> for a Namespace.</param>
/// <param name="Name">The name of the Resource.</param>
/// <param name="Action">Whether the Resource was created, changed or left as it was.</param>
/// <param name="Generation">The Generation the Resource is at after the apply.</param>
/// <param name="Changes">What changed in the Spec; empty when nothing did.</param>
public sealed record AppliedResource(
    string Kind,
    string? Namespace,
    string Name,
    ApplyAction Action,
    long Generation,
    IReadOnlyList<SpecChange> Changes);

/// <summary>
/// What an apply does to one Resource.
/// </summary>
public enum ApplyAction
{
    /// <summary>
    /// The Resource did not exist and was created at Generation 1.
    /// </summary>
    Created,

    /// <summary>
    /// The Resource's Spec changed, which started a new Generation.
    /// </summary>
    Configured,

    /// <summary>
    /// The Spec means the same as the current one, so nothing changed.
    /// </summary>
    Unchanged
}

internal sealed class Handler(
    IApplicationDbContext database,
    KindCatalog catalog,
    TimeProvider time,
    ISecretVault vault,
    IOptions<RevisionRetention> retention) : IRequestHandler<Command, Result<IReadOnlyList<AppliedResource>>>
{
    public async Task<Result<IReadOnlyList<AppliedResource>>> HandleAsync(
        Command request,
        CancellationToken cancellationToken = default)
    {
        var reading = ManifestReader.ForApply(request.Documents, request.Namespace, catalog);
        var access = await database.AuthoriseChangesAsync(request.CallerId, reading, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        if (request.Documents.Count == 0)
        {
            return Errors.EmptyManifest();
        }

        if (reading.Entries.Count == 0)
        {
            return new ManifestRejected(reading.Problems);
        }

        var state = await ManifestState.LoadAsync(database, catalog, reading.Entries, cancellationToken);
        if (state.IsFailure)
        {
            return state.Error;
        }

        var problems = reading.Problems.Concat(state.Value.Problems(reading.Entries))
            .OrderBy(problem => problem.Document ?? int.MaxValue)
            .ToList();
        if (problems.Count > 0)
        {
            return new ManifestRejected(problems);
        }

        var stale = state.Value.FirstStalePrecondition(reading.Entries);
        if (stale is not null)
        {
            return stale;
        }

        var author = new RevisionAuthor(request.CallerId, access.Value!.Username);
        var applied = new List<AppliedResource>();
        foreach (var entry in reading.Entries.Select(Seal))
        {
            applied.Add(
                entry.IsNamespace
                    ? await ApplyNamespaceAsync(entry, state.Value, author, request.DryRun, cancellationToken)
                    : await ApplyResourceAsync(entry, state.Value, author, request.DryRun, cancellationToken));
        }

        if (!request.DryRun && applied.Any(resource => resource.Action != ApplyAction.Unchanged))
        {
            await database.SaveChangesAsync(cancellationToken);
        }

        return Result.Found<IReadOnlyList<AppliedResource>>(applied);
    }

    private async Task<AppliedResource> ApplyNamespaceAsync(
        ManifestEntry entry,
        ManifestState state,
        RevisionAuthor author,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var existing = state.FindNamespace(entry.Namespace);
        if (existing is not null)
        {
            return Outcome(entry, ApplyAction.Unchanged, existing.Generation, []);
        }

        var created = Namespace.Create(entry.Namespace);
        if (!dryRun)
        {
            database.Namespaces.Add(created);
            await database.PublishAsync(created.ToApplied());
            await database.RecordAsync(
                Revision.Applied(created, author, time.GetUtcNow()),
                retention.Value.Keep,
                cancellationToken);
        }

        return Outcome(entry, ApplyAction.Created, created.Generation, SpecDiff.Between(null, Namespace.Spec));
    }

    private async Task<AppliedResource> ApplyResourceAsync(
        ManifestEntry entry,
        ManifestState state,
        RevisionAuthor author,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var existing = state.FindResource(entry);
        var changes = SpecDiff.Between(existing?.Spec, entry.Spec);
        if (entry.Values is not null)
        {
            changes = SecretKind.Hide(changes);
        }

        if (existing is not null && changes.Count == 0)
        {
            return Outcome(entry, ApplyAction.Unchanged, existing.Generation, changes);
        }

        var action = existing is null ? ApplyAction.Created : ApplyAction.Configured;
        if (dryRun)
        {
            return Outcome(entry, action, existing?.GenerationAfter(entry.Spec) ?? 1, changes);
        }

        var seq = state.FindNamespace(entry.Namespace)!.NextSeq();
        var resource = existing ?? Resource.Create(entry.Namespace, entry.Kind, entry.Name, entry.Spec, seq);
        if (existing is null)
        {
            database.Resources.Add(resource);
        }
        else
        {
            existing.Apply(entry.Spec, seq);
        }

        if (entry.Values is not null)
        {
            await database.StoreAsync(vault, entry, cancellationToken);
        }

        await database.PublishAsync(resource.ToApplied());
        await database.RecordAsync(
            Revision.Applied(resource, author, time.GetUtcNow()),
            retention.Value.Keep,
            cancellationToken);

        return Outcome(entry, action, resource.Generation, changes);
    }

    private ManifestEntry Seal(ManifestEntry entry) =>
        entry.Kind == SecretKind.KindName
            ? entry with
            {
                Spec = SecretKind.Seal(entry.Spec, vault.Fingerprint),
                Values = SecretKind.Values(entry.Spec)
            }
            : entry;

    private static AppliedResource Outcome(
        ManifestEntry entry,
        ApplyAction action,
        long generation,
        IReadOnlyList<SpecChange> changes) =>
        new(entry.Kind, entry.NamespaceOrNull, entry.Name.Value, action, generation, changes);
}
