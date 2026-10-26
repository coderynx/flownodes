using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Users.ListUsers;

/// <summary>
/// Lists Users. Only an Administrator can.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Q">Keeps only Users whose username contains this text, ignoring case; no filter when empty.</param>
/// <param name="Sort">The key to sort by. Only <c>username</c> exists; it is also the default.</param>
/// <param name="Desc">Reverses the sort order.</param>
/// <param name="Offset">How many Users to skip; negative values count as zero.</param>
/// <param name="Limit">How many Users to return; clamped to 1 through 200.</param>
public sealed record Query(Guid CallerId, string? Q, string? Sort, bool Desc, int Offset, int Limit)
    : IRequest<Result<UserPage>>;

/// <summary>
/// One page of a User listing.
/// </summary>
/// <param name="Items">The Users on this page.</param>
/// <param name="Total">How many Users match the filter, across all pages.</param>
public sealed record UserPage(IReadOnlyList<User> Items, int Total);

internal sealed class Handler(IIdentityDbContext database, ICallerChecks checks)
    : IRequestHandler<Query, Result<UserPage>>
{
    private const string UsernameKey = "username";
    private const int MaxLimit = 200;

    public async Task<Result<UserPage>> HandleAsync(Query request, CancellationToken cancellationToken = default)
    {
        var caller = await checks.RequireAdministratorAsync(request.CallerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        if (request.Sort is not null && request.Sort != UsernameKey)
        {
            return Errors.UnknownSortKey(request.Sort);
        }

        var all = await database.Users.AsNoTracking().Include(user => user.RoleGrants).ToListAsync(cancellationToken);

        var matching = all.Where(user => string.IsNullOrEmpty(request.Q) ||
                                         user.Username.Value.Contains(request.Q, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var ordered = request.Desc
            ? matching.OrderByDescending(user => user.Username.Normalized, StringComparer.Ordinal)
            : matching.OrderBy(user => user.Username.Normalized, StringComparer.Ordinal);
        var items = ordered.Skip(Math.Max(request.Offset, 0)).Take(Math.Clamp(request.Limit, 1, MaxLimit)).ToList();

        return Result.Found(new UserPage(items, matching.Count));
    }
}
