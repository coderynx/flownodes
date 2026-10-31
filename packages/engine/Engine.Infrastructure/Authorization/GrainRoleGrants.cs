using Coderynx.Functional.Results;
using Core.Application.Authorization;
using Core.Domain.Authorization;
using Engine.Application.Authorization;

namespace Engine.Infrastructure.Authorization;

internal sealed class GrainRoleGrants(IGrainFactory grains, UnknownCallerResync unknownCallers) : IRoleGrants
{
    public async Task<Result> RequireRoleAsync(
        Guid callerId,
        string @namespace,
        NamespaceRole role,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(callerId, cancellationToken);

        return Access.Authorise(caller.IsActive, @namespace, RolesIn(caller, @namespace), role);
    }

    public async Task<Result> RequireMemberAsync(Guid callerId, string @namespace, CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(callerId, cancellationToken);

        return Access.Authorise(caller.IsActive, @namespace, RolesIn(caller, @namespace), null);
    }

    public async Task<Result> RequireAnyMembershipAsync(Guid callerId, CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(callerId, cancellationToken);

        return Access.AuthoriseMember(caller.IsActive, caller.Grants.Count);
    }

    private async Task<UserAccessSnapshot> CallerAsync(Guid callerId, CancellationToken cancellationToken)
    {
        var caller = await grains.GetGrain<IUserAccessGrain>(callerId).GetAsync();
        if (!caller.IsKnown)
        {
            await unknownCallers.RequestAsync(callerId, cancellationToken);
        }

        return caller;
    }

    private static List<NamespaceRole> RolesIn(UserAccessSnapshot caller, string @namespace) =>
        [.. caller.Grants.Where(grant => grant.Namespace == @namespace).Select(grant => grant.Role)];
}
