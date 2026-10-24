using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Grants.RevokeRole;

/// <summary>
/// Revokes the Administrator role, or the Engineer or Operator role in a Namespace. Only an Administrator can, and
/// never from the last active Administrator.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="UserId">The id of the User who loses the role.</param>
/// <param name="Role"><c>Administrator</c>, <c>Engineer</c> or <c>Operator</c>.</param>
/// <param name="Namespace">The Namespace of an Engineer or Operator role; none for Administrator.</param>
public sealed record Command(Guid CallerId, Guid UserId, string? Role, string? Namespace) : IRequest<Result>;

internal sealed class Handler(IIdentityDbContext database) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var caller = await Callers.RequireAdministratorAsync(database, request.CallerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        var spec = RoleSpec.Parse(request.Role, request.Namespace);
        if (spec.IsFailure)
        {
            return spec.Error;
        }

        var user = await database.Users.Include(candidate => candidate.RoleGrants)
            .SingleOrDefaultAsync(candidate => candidate.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return Errors.UserNotFound();
        }

        if (spec.Value.IsAdministrator)
        {
            return await RevokeAdministratorAsync(user, cancellationToken);
        }

        if (!user.Revoke(spec.Value.Namespace, spec.Value.Role))
        {
            return Errors.RoleNotHeld();
        }

        await database.PublishAsync(user.ToRoleRevoked(spec.Value.Namespace, spec.Value.Role));
        await database.SaveChangesAsync(cancellationToken);

        return Result.Deleted();
    }

    private async Task<Result> RevokeAdministratorAsync(Domain.Users.User user, CancellationToken cancellationToken)
    {
        if (!user.IsAdministrator)
        {
            return Errors.RoleNotHeld();
        }

        if (!user.IsDisabled &&
            !await Callers.AnotherActiveAdministratorExistsAsync(database, user.Id, cancellationToken))
        {
            return Errors.LastAdministrator();
        }

        user.RevokeAdministrator();
        await database.PublishAsync(user.ToAdministratorRevoked());
        await database.SaveChangesAsync(cancellationToken);

        return Result.Deleted();
    }
}
