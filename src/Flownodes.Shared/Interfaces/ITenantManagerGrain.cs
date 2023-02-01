namespace Flownodes.Shared.Interfaces;

public interface ITenantManagerGrain : IGrainWithStringKey
{
    ValueTask<ITenantGrain?> GetTenantAsync(string id);
    ValueTask<IList<ITenantGrain>> GetTenantsAsync();
    ValueTask<ITenantGrain?> CreateTenantAsync(string id, Dictionary<string, string?>? metadata = null);
    Task RemoveTenantAsync(string id);
}