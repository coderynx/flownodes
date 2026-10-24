using Identity.Domain.Users;

namespace Identity.Presentation.Users.Contracts;

internal sealed record RoleGrantDto(string Namespace, string Role);

internal sealed record UserDto(
    Guid Id,
    string Username,
    string? DisplayName,
    bool Administrator,
    bool Disabled,
    IReadOnlyList<RoleGrantDto> RoleGrants);

internal static class UserMappings
{
    public static UserDto ToDto(this User user) =>
        new(
            user.Id,
            user.Username.Value,
            user.DisplayName,
            user.IsAdministrator,
            user.IsDisabled,
            [.. user.RoleGrants.Select(grant => new RoleGrantDto(grant.Namespace, grant.Role.ToString()))]);
}
