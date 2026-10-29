using Core.Contracts.Identity;
using Identity.Domain.Users;

namespace Identity.Application;

internal static class UserMessages
{
    public static RoleGranted ToRoleGranted(this User user, string @namespace, Role role) =>
        new(user.Id, @namespace, role.ToString(), user.Seq);

    public static RoleRevoked ToRoleRevoked(this User user, string @namespace, Role role) =>
        new(user.Id, @namespace, role.ToString(), user.Seq);

    public static RoleRevoked ToRoleRevoked(this User user, string @namespace, RoleRevocation revocation) =>
        new(user.Id, @namespace, revocation.Role.ToString(), revocation.Seq);

    public static AdministratorGranted ToAdministratorGranted(this User user) => new(user.Id, user.Seq);
    public static AdministratorRevoked ToAdministratorRevoked(this User user) => new(user.Id, user.Seq);
    public static UserDisabled ToDisabled(this User user) => new(user.Id, user.Seq);
    public static UserEnabled ToEnabled(this User user) => new(user.Id, user.Seq);
    public static UserDeleted ToDeleted(this User user) => new(user.Id, user.Seq);
    public static UserRenamed ToRenamed(this User user) => new(user.Id, user.Username.Value, user.Seq);

    public static UserAccessPublished ToAccessPublished(this User user) =>
        new(
            user.Id,
            user.Username.Value,
            user.IsDisabled,
            false,
            user.IsAdministrator,
            [.. user.RoleGrants.Select(grant => new PublishedRoleGrant(grant.Namespace, grant.Role.ToString()))],
            user.Seq);

    public static UserAccessPublished ToAccessPublished(this DeletedUser user) =>
        new(user.Id, null, false, true, false, [], user.Seq);
}
