using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Users.EnableUser;

/// <summary>
/// Enables a disabled User. Only an Administrator can.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="UserId">The id of the User to enable.</param>
public sealed record Command(Guid CallerId, Guid UserId) : IRequest<Result>;

internal sealed class Handler(IIdentityDbContext database) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var caller = await Callers.RequireAdministratorAsync(database, request.CallerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        var user = await database.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == request.UserId,
            cancellationToken);
        if (user is null)
        {
            return Errors.UserNotFound();
        }

        if (user.Enable())
        {
            await database.PublishAsync(user.ToEnabled());
        }

        await database.SaveChangesAsync(cancellationToken);

        return Result.Updated();
    }
}
