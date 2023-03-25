using Flownodes.Shared.Authentication.Models;

namespace Flownodes.Worker.Authentication.Surrogates;

[GenerateSerializer]
internal struct ApplicationRoleClaimSurrogate
{
    public ApplicationRoleClaimSurrogate()
    {
    }

    [Id(0)] public int Id { get; init; } = default!;
    [Id(1)] public string RoleId { get; init; } = default!;
    [Id(2)] public string? ClaimType { get; init; }
    [Id(3)] public string? ClaimValue { get; init; }
}

[RegisterConverter]
internal sealed class
    ApplicationRoleClaimSurrogateConverter : IConverter<ApplicationRoleClaim, ApplicationRoleClaimSurrogate>
{
    public ApplicationRoleClaim ConvertFromSurrogate(in ApplicationRoleClaimSurrogate surrogate)
    {
        return new ApplicationRoleClaim
        {
            Id = surrogate.Id,
            RoleId = surrogate.RoleId,
            ClaimType = surrogate.ClaimType,
            ClaimValue = surrogate.ClaimValue
        };
    }

    public ApplicationRoleClaimSurrogate ConvertToSurrogate(in ApplicationRoleClaim value)
    {
        return new ApplicationRoleClaimSurrogate
        {
            Id = value.Id,
            RoleId = value.RoleId,
            ClaimType = value.ClaimType,
            ClaimValue = value.ClaimValue
        };
    }
}