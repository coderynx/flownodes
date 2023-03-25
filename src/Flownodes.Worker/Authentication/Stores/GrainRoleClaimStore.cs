using System.Security.Claims;
using Flownodes.Shared.Authentication;
using Flownodes.Shared.Authentication.Models;
using Flownodes.Worker.Services;
using Microsoft.AspNetCore.Identity;

namespace Flownodes.Worker.Authentication.Stores;

public class GrainRoleClaimStore : IRoleClaimStore<ApplicationRole>
{
    private readonly IRoleClaimManagerGrain _roleClaimManager;

    public GrainRoleClaimStore(IEnvironmentService environmentService)
    {
        _roleClaimManager = environmentService.GetRoleClaimManager();
    }

    public void Dispose()
    {
    }

    public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        cancellationToken.ThrowIfCancellationRequested();

        await _roleClaimManager.AddRoleAsync(role);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        cancellationToken.ThrowIfCancellationRequested();

        if (await _roleClaimManager.UpdateRoleAsync(role)) return IdentityResult.Success;

        return IdentityResult.Failed();
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        cancellationToken.ThrowIfCancellationRequested();

        if (await _roleClaimManager.DeleteRoleAsync(role.Id)) return IdentityResult.Success;

        return IdentityResult.Failed();
    }

    public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(role.Id);
    }

    public Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(role.Name);
    }

    public Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        cancellationToken.ThrowIfCancellationRequested();

        role.Name = roleName;

        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(role.NormalizedName);
    }

    public Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        cancellationToken.ThrowIfCancellationRequested();

        role.NormalizedName = normalizedName;

        return Task.CompletedTask;
    }

    public async Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _roleClaimManager.GetRoleById(roleId);
    }

    public async Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _roleClaimManager.FindByNormalizedNameAsync(normalizedRoleName);
    }

    public async Task<IList<Claim>> GetClaimsAsync(ApplicationRole role, CancellationToken cancellationToken = new())
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(role);

        var claims = await _roleClaimManager.GetApplicationRoleClaims(role.Id);

        // TODO: Review nullables.
        return claims.Select(c => new Claim(c.ClaimType!, c.ClaimValue!)).ToList();
    }

    public async Task AddClaimAsync(ApplicationRole role, Claim claim, CancellationToken cancellationToken = new())
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(claim);

        var applicationRoleClaim = new ApplicationRoleClaim
        {
            ClaimType = claim.Type,
            ClaimValue = claim.Value
        };

        await _roleClaimManager.AddRoleClaimAsync(role.Id, applicationRoleClaim);
    }

    public async Task RemoveClaimAsync(ApplicationRole role, Claim claim, CancellationToken cancellationToken = new())
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(claim);

        await _roleClaimManager.DeleteRoleClaimAsync(role.Id, claim.Type, claim.Value);
    }
}