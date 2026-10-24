using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Application.Authorization;
using Core.Contracts.Identity;
using Core.Domain.Authorization;
using Registry.Application.Database;

namespace Registry.Application.Access.ApplyIdentityMessage;

/// <summary>
/// Applies one of Identity's messages about a User or a Role Grant to the Registry's replica, which every request is
/// authorised from (ADR-0026). Any other object changes nothing.
/// </summary>
/// <param name="Message">One of the <c>Core.Contracts.Identity</c> messages.</param>
public sealed record Command(object Message) : IRequest<Result>;

internal sealed class Handler(IApplicationDbContext database) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        switch (request.Message)
        {
            case RoleGranted granted:
                await Apply(granted.UserId, user => user.GrantRole(granted.Namespace, Role(granted.Role), granted.Seq));
                break;
            case RoleRevoked revoked:
                await Apply(
                    revoked.UserId,
                    user => user.RevokeRole(revoked.Namespace, Role(revoked.Role), revoked.Seq));
                break;
            case AdministratorGranted granted:
                await Apply(granted.UserId, user => user.GrantAdministrator(granted.Seq));
                break;
            case AdministratorRevoked revoked:
                await Apply(revoked.UserId, user => user.RevokeAdministrator(revoked.Seq));
                break;
            case UserDisabled disabled:
                await Apply(disabled.UserId, user => user.Disable(disabled.Seq));
                break;
            case UserEnabled enabled:
                await Apply(enabled.UserId, user => user.Enable(enabled.Seq));
                break;
            case UserDeleted deleted:
                await Apply(deleted.UserId, user => user.Delete(deleted.Seq));
                break;
            case UserRenamed renamed:
                await Apply(renamed.UserId, user => user.Rename(renamed.Username, renamed.Seq));
                break;
        }

        return Result.Updated();

        Task Apply(Guid userId, Action<UserAccess> change) => database.ApplyAsync(userId, change, cancellationToken);
    }

    private static NamespaceRole Role(string role) => Enum.Parse<NamespaceRole>(role);
}
