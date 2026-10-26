using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Application.Authorization;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces.ListNamespaces;

/// <summary>
/// Lists the Namespaces the caller may see: every one for an Administrator, otherwise those where the caller holds a
/// role.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Q">Keeps only Namespaces whose name contains this text; no filter when empty.</param>
/// <param name="Sort">The key to sort by. Only <c>name</c> exists; it is also the default.</param>
/// <param name="Desc">Reverses the sort order.</param>
/// <param name="Offset">How many Namespaces to skip; negative values count as zero.</param>
/// <param name="Limit">How many Namespaces to return; clamped to 1 through 200.</param>
public sealed record Query(Guid CallerId, string? Q, string? Sort, bool Desc, int Offset, int Limit)
    : IRequest<Result<NamespacePage>>;

/// <summary>
/// One page of a Namespace listing.
/// </summary>
/// <param name="Items">The Namespaces on this page.</param>
/// <param name="Total">How many Namespaces match the filter, across all pages.</param>
public sealed record NamespacePage(IReadOnlyList<Namespace> Items, int Total);

internal sealed class Handler(IApplicationDbContext database, IAccessChecks access)
    : IRequestHandler<Query, Result<NamespacePage>>
{
    private const string NameKey = "name";

    public async Task<Result<NamespacePage>> HandleAsync(Query request, CancellationToken cancellationToken = default)
    {
        if (request.Sort is not null && request.Sort != NameKey)
        {
            return Errors.UnknownSortKey(request.Sort);
        }

        var scope = await access.ScopeAsync(request.CallerId, cancellationToken);
        if (scope.IsFailure)
        {
            return scope.Error;
        }

        var all = await database.Namespaces.AsNoTracking().ToListAsync(cancellationToken);

        var matching = all.Where(@namespace => scope.Value.Includes(@namespace.Name.Value) &&
                                               (string.IsNullOrEmpty(request.Q) || @namespace.Name.Value.Contains(
                                                   request.Q,
                                                   StringComparison.Ordinal)))
            .ToList();
        var ordered = request.Desc
            ? matching.OrderByDescending(@namespace => @namespace.Name.Value, StringComparer.Ordinal)
            : matching.OrderBy(@namespace => @namespace.Name.Value, StringComparer.Ordinal);
        var items = ordered.Page(request.Offset, request.Limit).ToList();

        return Result.Found(new NamespacePage(items, matching.Count));
    }
}
