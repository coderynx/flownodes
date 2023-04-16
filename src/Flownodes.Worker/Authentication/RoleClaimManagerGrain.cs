using Flownodes.Sdk.Entities;
using Flownodes.Shared.Authentication;
using Flownodes.Shared.Authentication.Models;
using Orleans.Runtime;

namespace Flownodes.Worker.Authentication;

[GrainType(EntityNames.RoleClaimManager)]
public class RoleClaimManagerGrain : Grain, IRoleClaimManagerGrain
{
    private readonly ILogger<RoleClaimManagerGrain> _logger;
    private readonly IPersistentState<Dictionary<string, HashSet<ApplicationRoleClaim>>> _roleClaimStore;
    private readonly IPersistentState<List<ApplicationRole>> _roleStore;

    public RoleClaimManagerGrain(ILogger<RoleClaimManagerGrain> logger,
        [PersistentState("roleStore")] IPersistentState<List<ApplicationRole>> roleStore,
        [PersistentState("roleClaimStore")]
        IPersistentState<Dictionary<string, HashSet<ApplicationRoleClaim>>> roleClaimStore)
    {
        _logger = logger;
        _roleStore = roleStore;
        _roleClaimStore = roleClaimStore;
    }

    public async Task AddRoleAsync(ApplicationRole role)
    {
        _roleStore.State.Add(role);
        await _roleStore.WriteStateAsync();

        _logger.LogInformation("Added role {@RoleName} to RoleManagerGrain", role.Name);
    }

    public async ValueTask<bool> UpdateRoleAsync(ApplicationRole role)
    {
        var index = _roleStore.State.FindIndex(r => r.Id.Equals(role.Id));
        if (index < 0) return false;

        _roleStore.State[index] = role;
        await _roleStore.WriteStateAsync();

        _logger.LogInformation("Updated role {@RoleName}", role.Name);
        return true;
    }

    public async ValueTask<bool> DeleteRoleAsync(string id)
    {
        var index = _roleStore.State.FindIndex(r => r.Id.Equals(id));
        if (index < 0) return false;

        _roleStore.State.RemoveAt(index);
        await _roleStore.WriteStateAsync();

        _logger.LogInformation("Deleted role {@RoleName}", id);
        return true;
    }

    public ValueTask<ApplicationRole?> GetRoleById(string id)
    {
        return ValueTask.FromResult(_roleStore.State.SingleOrDefault(r => r.Id.Equals(id)));
    }

    public ValueTask<ApplicationRole?> FindByNormalizedNameAsync(string normalizedName)
    {
        var record = _roleStore.State
            .SingleOrDefault(role =>
                role.NormalizedName is not null && role.NormalizedName.Equals(normalizedName));
        return ValueTask.FromResult(record);
    }

    public ValueTask<IEnumerable<ApplicationRoleClaim>> GetApplicationRoleClaims(string roleId)
    {
        var claims = _roleClaimStore.State.GetValueOrDefault(roleId);
        return ValueTask.FromResult(claims ?? Enumerable.Empty<ApplicationRoleClaim>());
    }

    public async Task AddRoleClaimAsync(string roleId, ApplicationRoleClaim applicationRoleClaim)
    {
        if (!_roleClaimStore.State.ContainsKey(roleId))
            _roleClaimStore.State.Add(roleId, new HashSet<ApplicationRoleClaim>());

        var entry = _roleClaimStore.State[roleId];
        entry.Add(applicationRoleClaim);

        await _roleClaimStore.WriteStateAsync();
        _logger.LogInformation("Added role claim {@RoleClaimType} to RoleClaimManager", applicationRoleClaim.ClaimType);
    }

    public async Task DeleteRoleClaimAsync(string roleId, string claimType, string claimValue)
    {
        if (!_roleClaimStore.State.ContainsKey(roleId)) return;

        var entry = _roleClaimStore.State[roleId];
        entry.RemoveWhere(roleClaim =>
            roleClaim.ClaimType!.Equals(claimType) && roleClaim.ClaimValue!.Equals(claimValue));
        await _roleClaimStore.WriteStateAsync();

        _logger.LogInformation("Deleted role claim {@RoleClaimType} from RoleClaimManager", claimType);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activated RolesManagerGrain");
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivated RolesManagerGrain for reason {@DeactivationReason}", reason.Description);
        return Task.CompletedTask;
    }
}