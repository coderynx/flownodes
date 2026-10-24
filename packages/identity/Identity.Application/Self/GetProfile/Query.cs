using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Users;

namespace Identity.Application.Self.GetProfile;

/// <summary>
/// Reads the caller's own User, with their Role Grants.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
public sealed record Query(Guid CallerId) : IRequest<Result<User>>;

internal sealed class Handler(IIdentityDbContext database) : IRequestHandler<Query, Result<User>>
{
    public async Task<Result<User>> HandleAsync(Query request, CancellationToken cancellationToken = default) =>
        await Callers.RequireActiveAsync(database, request.CallerId, cancellationToken);
}
