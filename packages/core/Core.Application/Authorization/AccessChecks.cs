using Coderynx.Functional.Results;
using Core.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Authorization;

internal sealed class AccessChecks(IUserAccessSource source) : IAccessChecks
{
    public async Task<Result<UserAccess>> RequireAdministratorAsync(
        Guid callerId,
        CancellationToken cancellationToken = default)
    {
        var caller = await FindActiveAsync(callerId, cancellationToken);

        return caller.IsSuccess && !caller.Value.IsAdministrator ? AccessErrors.NotAdministrator() : caller;
    }

    public async Task<Result<UserAccess>> RequireRoleAsync(
        Guid callerId,
        string @namespace,
        NamespaceRole role,
        CancellationToken cancellationToken = default)
    {
        var caller = await RequireMemberAsync(callerId, @namespace, cancellationToken);

        return caller.IsSuccess && !caller.Value.Holds(@namespace, role)
            ? AccessErrors.MissingRole(@namespace, role.ToString())
            : caller;
    }

    public async Task<Result<UserAccess>> RequireMemberAsync(
        Guid callerId,
        string @namespace,
        CancellationToken cancellationToken = default)
    {
        var caller = await FindActiveAsync(callerId, cancellationToken);

        return caller.IsSuccess && !caller.Value.Holds(@namespace, null)
            ? AccessErrors.NamespaceNotFound(@namespace)
            : caller;
    }

    public async Task<Result<NamespaceScope>> ScopeAsync(Guid callerId, CancellationToken cancellationToken = default)
    {
        var caller = await FindActiveAsync(callerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        var names = caller.Value.Grants.Select(grant => grant.Namespace).ToHashSet();

        return Result.Found(new NamespaceScope(caller.Value.IsAdministrator, names));
    }

    private async Task<Result<UserAccess>> FindActiveAsync(Guid callerId, CancellationToken cancellationToken)
    {
        var caller = await source.UserAccess.AsNoTracking()
            .Include(user => user.Grants)
            .SingleOrDefaultAsync(user => user.Id == callerId, cancellationToken);

        return caller is { IsActive: true } ? Result.Found(caller) : AccessErrors.Unauthenticated();
    }
}
