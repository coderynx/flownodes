using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.SignIn.FindSignInUser;

/// <summary>
/// Finds the User a token was issued to, as long as that User may still obtain tokens.
/// </summary>
/// <param name="UserId">The subject of the token.</param>
public sealed record Query(Guid UserId) : IRequest<Result<User>>;

internal sealed class Handler(IIdentityDbContext database) : IRequestHandler<Query, Result<User>>
{
    public async Task<Result<User>> HandleAsync(Query request, CancellationToken cancellationToken = default)
    {
        var user = await database.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == request.UserId, cancellationToken);

        return user is { IsDisabled: false } ? Result.Found(user) : Errors.InvalidCredentials();
    }
}
