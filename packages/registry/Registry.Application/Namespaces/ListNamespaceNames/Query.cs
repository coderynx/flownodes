using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;

namespace Registry.Application.Namespaces.ListNamespaceNames;

/// <summary>
/// Lists the name of every Namespace, because the Engine asked for it (ADR-0014). It acts for the Engine, not for a
/// caller.
/// </summary>
public sealed record Query : IRequest<Result<IReadOnlyList<string>>>;

internal sealed class Handler(IApplicationDbContext database) : IRequestHandler<Query, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> HandleAsync(
        Query request,
        CancellationToken cancellationToken = default)
    {
        var namespaces = await database.Namespaces.AsNoTracking().ToListAsync(cancellationToken);

        return Result.Found<IReadOnlyList<string>>([.. namespaces.Select(@namespace => @namespace.Name.Value)]);
    }
}
