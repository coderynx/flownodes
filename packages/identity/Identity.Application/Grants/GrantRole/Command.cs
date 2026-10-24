using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Namespaces;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Grants.GrantRole;

/// <summary>
/// Grants the Administrator role, or the Engineer or Operator role in a Namespace that exists. Only an Administrator
/// can.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="UserId">The id of the User who receives the role.</param>
/// <param name="Role"><c>Administrator</c>, <c>Engineer</c> or <c>Operator</c>.</param>
/// <param name="Namespace">The Namespace of an Engineer or Operator role; none for Administrator.</param>
public sealed record Command(Guid CallerId, Guid UserId, string? Role, string? Namespace) : IRequest<Result<User>>;

internal sealed class Handler(IIdentityDbContext database) : IRequestHandler<Command, Result<User>>
{
    public async Task<Result<User>> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var caller = await CallerChecks.RequireAdministratorAsync(database, request.CallerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        var spec = RoleSpec.Parse(request.Role, request.Namespace);
        if (spec.IsFailure)
        {
            return spec.Error;
        }

        var found = await CallerChecks.FindUserAsync(database, request.UserId, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        var user = found.Value;

        if (spec.Value.IsAdministrator)
        {
            return await GrantAdministratorAsync(user, cancellationToken);
        }

        var known = spec.Value.Namespace == KnownNamespace.Default || await database.KnownNamespaces.AnyAsync(
            candidate => candidate.Name == spec.Value.Namespace,
            cancellationToken);
        if (!known)
        {
            return Errors.UnknownNamespace(spec.Value.Namespace);
        }

        if (!user.Grant(spec.Value.Namespace, spec.Value.Role))
        {
            return Result.Found(user);
        }

        await database.PublishAsync(user.ToRoleGranted(spec.Value.Namespace, spec.Value.Role));
        await database.SaveChangesAsync(cancellationToken);

        return Result.Created(user);
    }

    private async Task<Result<User>> GrantAdministratorAsync(User user, CancellationToken cancellationToken)
    {
        if (!user.GrantAdministrator())
        {
            return Result.Found(user);
        }

        await database.PublishAsync(user.ToAdministratorGranted());
        await database.SaveChangesAsync(cancellationToken);

        return Result.Created(user);
    }
}
