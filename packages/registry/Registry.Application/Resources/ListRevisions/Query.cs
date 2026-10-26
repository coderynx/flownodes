using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Application.Authorization;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Application.Resources.ListRevisions;

/// <summary>
/// Lists the Revisions of one Resource, newest first, including those of earlier incarnations and the tombstones of
/// deletes. A namespaced Resource's Revisions need any role in its Namespace; a Namespace's need a role in it or the
/// Administrator role.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Resource, or <see langword="null"/> for a Namespace's own.</param>
/// <param name="Kinds">The Kind as a route writes it, lowercase and plural, such as <c>connections</c>.</param>
/// <param name="Name">The name of the Resource. A name that is not valid names no Resource.</param>
/// <param name="Offset">How many Revisions to skip; negative values count as zero.</param>
/// <param name="Limit">How many Revisions to return; clamped to 1 through 200.</param>
public sealed record Query(Guid CallerId, string? Namespace, string Kinds, string Name, int Offset, int Limit)
    : IRequest<Result<RevisionPage>>;

/// <summary>
/// One page of a Resource's Revisions.
/// </summary>
/// <param name="Items">The Revisions on this page, newest first.</param>
/// <param name="Total">How many Revisions the Resource has, across all pages.</param>
public sealed record RevisionPage(IReadOnlyList<Revision> Items, int Total);

internal sealed class Handler(IApplicationDbContext database, IAccessChecks access, IResourceAccess resourceAccess)
    : IRequestHandler<Query, Result<RevisionPage>>
{
    public async Task<Result<RevisionPage>> HandleAsync(Query request, CancellationToken cancellationToken = default)
    {
        var subject = request.Namespace is null && KindRoutes.IsNamespace(request.Kinds)
            ? await NamespaceSubjectAsync(request, cancellationToken)
            : await ResourceSubjectAsync(request, cancellationToken);
        if (subject.IsFailure)
        {
            return subject.Error;
        }

        var (@namespace, kind) = subject.Value;
        if (!ResourceName.TryCreate(request.Name, out var name))
        {
            return Errors.NotFound(kind, request.Name);
        }

        var revisions = database.Revisions.AsNoTracking()
            .Where(revision => revision.Namespace == @namespace && revision.Kind == kind && revision.Name == name);
        var total = await revisions.CountAsync(cancellationToken);
        if (total == 0)
        {
            return Errors.NotFound(kind, request.Name);
        }

        var items = await revisions.OrderByDescending(revision => revision.AppliedAt)
            .ThenByDescending(revision => revision.Id)
            .Page(request.Offset, request.Limit)
            .ToListAsync(cancellationToken);

        return Result.Found(new RevisionPage(items, total));
    }

    private async Task<Result<(NamespaceName Namespace, string Kind)>> NamespaceSubjectAsync(
        Query request,
        CancellationToken cancellationToken)
    {
        var scope = await access.ScopeAsync(request.CallerId, cancellationToken);
        if (scope.IsFailure)
        {
            return scope.Error;
        }

        return scope.Value.Includes(request.Name) && NamespaceName.TryCreate(request.Name, out var name)
            ? Result.Found((name, Namespace.Kind))
            : Errors.NamespaceNotFound(request.Name);
    }

    private async Task<Result<(NamespaceName Namespace, string Kind)>> ResourceSubjectAsync(
        Query request,
        CancellationToken cancellationToken)
    {
        if (request.Namespace is null)
        {
            var scope = await access.ScopeAsync(request.CallerId, cancellationToken);

            return scope.IsFailure ? scope.Error : Errors.KindNotFound(request.Kinds);
        }

        var read = await resourceAccess.AuthoriseReadAsync(
            request.CallerId,
            request.Namespace,
            request.Kinds,
            cancellationToken);

        return read.IsFailure ? read.Error : Result.Found((read.Value.Namespace, read.Value.Kind.Name));
    }
}
