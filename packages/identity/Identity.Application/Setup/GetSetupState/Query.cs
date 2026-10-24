using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Setup.GetSetupState;

/// <summary>
/// Asks whether the installation still waits for its first User.
/// </summary>
public sealed record Query : IRequest<Result<bool>>;

internal sealed class Handler(IIdentityDbContext database) : IRequestHandler<Query, Result<bool>>
{
    public async Task<Result<bool>> HandleAsync(Query request, CancellationToken cancellationToken = default) =>
        Result.Found(!await database.Users.AnyAsync(cancellationToken));
}
