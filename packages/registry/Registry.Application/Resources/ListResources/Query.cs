using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Resources;

namespace Registry.Application.Resources.ListResources;

/// <summary>
/// Lists the Resources of one Kind in a Namespace. The caller must hold the Operator or Engineer role in it.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The name of the Namespace.</param>
/// <param name="Kinds">The Kind as a route writes it, lowercase and plural, such as <c>connections</c>.</param>
/// <param name="Q">Keeps only Resources whose name contains this text; no filter when empty.</param>
/// <param name="Sort">The key to sort by. Only <c>name</c> exists; it is also the default.</param>
/// <param name="Desc">Reverses the sort order.</param>
/// <param name="Offset">How many Resources to skip; negative values count as zero.</param>
/// <param name="Limit">How many Resources to return; clamped to 1 through 200.</param>
public sealed record Query(
    Guid CallerId,
    string Namespace,
    string Kinds,
    string? Q,
    string? Sort,
    bool Desc,
    int Offset,
    int Limit) : IRequest<Result<ResourcePage>>;

/// <summary>
/// One page of a Resource listing.
/// </summary>
/// <param name="Items">The Resources on this page.</param>
/// <param name="Total">How many Resources match the filter, across all pages.</param>
public sealed record ResourcePage(IReadOnlyList<Resource> Items, int Total);

internal sealed class Handler(IApplicationDbContext database, IResourceAccess access)
    : IRequestHandler<Query, Result<ResourcePage>>
{
    private const string NameKey = "name";

    public async Task<Result<ResourcePage>> HandleAsync(Query request, CancellationToken cancellationToken = default)
    {
        var read = await access.AuthoriseReadAsync(
            request.CallerId,
            request.Namespace,
            request.Kinds,
            cancellationToken);
        if (read.IsFailure)
        {
            return read.Error;
        }

        if (request.Sort is not null && request.Sort != NameKey)
        {
            return Errors.UnknownSortKey(request.Sort);
        }

        var (kind, @namespace) = read.Value;
        var all = await database.Resources.AsNoTracking()
            .Where(resource => resource.Namespace == @namespace && resource.Kind == kind.Name)
            .ToListAsync(cancellationToken);

        var matching = all.Where(resource => string.IsNullOrEmpty(request.Q) ||
                                             resource.Name.Value.Contains(request.Q, StringComparison.Ordinal))
            .ToList();
        var ordered = request.Desc
            ? matching.OrderByDescending(resource => resource.Name.Value, StringComparer.Ordinal)
            : matching.OrderBy(resource => resource.Name.Value, StringComparer.Ordinal);
        var items = ordered.Page(request.Offset, request.Limit).ToList();

        return Result.Found(new ResourcePage(items, matching.Count));
    }
}
