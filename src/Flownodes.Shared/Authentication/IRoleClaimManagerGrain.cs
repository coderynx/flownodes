using Flownodes.Shared.Authentication.Models;

namespace Flownodes.Shared.Authentication;

public interface IRoleClaimManagerGrain : IGrainWithStringKey
{
    Task AddRoleAsync(ApplicationRole role);
    ValueTask<bool> UpdateRoleAsync(ApplicationRole role);
    ValueTask<bool> DeleteRoleAsync(string id);
    ValueTask<ApplicationRole?> GetRoleById(string id);
    ValueTask<ApplicationRole?> FindByNormalizedNameAsync(string normalizedName);
    ValueTask<IEnumerable<ApplicationRoleClaim>> GetApplicationRoleClaims(string roleId);
    Task AddRoleClaimAsync(string roleId, ApplicationRoleClaim applicationRoleClaim);
    Task DeleteRoleClaimAsync(string roleId, string claimType, string claimValue);
}