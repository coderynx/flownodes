namespace Flownodes.Shared.Tenanting;

public interface ITenantManagerGrain : IEntityGrain
{
    ValueTask<ITenantGrain?> GetTenantAsync(string name);
    ValueTask<IList<ITenantGrain>> GetTenantsAsync();
    ValueTask<ITenantGrain> CreateTenantAsync(string name, IDictionary<string, string?>? metadata = null);
    Task RemoveTenantAsync(string name);
    ValueTask<bool> IsTenantRegistered(string tenantName);
}