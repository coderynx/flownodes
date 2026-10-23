using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces.ListNamespaces;

/// <summary>
/// Lists Namespaces.
/// </summary>
/// <param name="Q">Keeps only Namespaces whose name contains this text; no filter when empty.</param>
/// <param name="Sort">The key to sort by. Only <c>name</c> exists; it is also the default.</param>
/// <param name="Desc">Reverses the sort order.</param>
/// <param name="Offset">How many Namespaces to skip; negative values count as zero.</param>
/// <param name="Limit">How many Namespaces to return; clamped to 1 through 200.</param>
public sealed record Query(string? Q, string? Sort, bool Desc, int Offset, int Limit) : IRequest<Result<NamespacePage>>;

/// <summary>
/// One page of a Namespace listing.
/// </summary>
/// <param name="Items">The Namespaces on this page.</param>
/// <param name="Total">How many Namespaces match the filter, across all pages.</param>
public sealed record NamespacePage(IReadOnlyList<Namespace> Items, int Total);

internal sealed class Handler(IApplicationDbContext database) : IRequestHandler<Query, Result<NamespacePage>>
{
    private const string NameKey = "name";
    private const int MaxLimit = 200;

    public async Task<Result<NamespacePage>> HandleAsync(Query request, CancellationToken cancellationToken = default)
    {
        if (request.Sort is not null && request.Sort != NameKey)
        {
            return Errors.UnknownSortKey(request.Sort);
        }

        var all = await database.Namespaces.AsNoTracking().ToListAsync(cancellationToken);

        var matching = all.Where(@namespace => string.IsNullOrEmpty(request.Q) ||
                                               @namespace.Name.Value.Contains(request.Q, StringComparison.Ordinal))
            .ToList();
        var ordered = request.Desc
            ? matching.OrderByDescending(@namespace => @namespace.Name.Value, StringComparer.Ordinal)
            : matching.OrderBy(@namespace => @namespace.Name.Value, StringComparer.Ordinal);
        var items = ordered.Skip(Math.Max(request.Offset, 0)).Take(Math.Clamp(request.Limit, 1, MaxLimit)).ToList();

        return Result.Found(new NamespacePage(items, matching.Count));
    }
}
