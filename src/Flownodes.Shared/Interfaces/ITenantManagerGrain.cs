namespace Flownodes.Shared.Interfaces;

public interface ITenantManagerGrain : IGrainWithStringKey
{
    ValueTask<ITenantGrain?> GetTenantAsync(string id);
    Task GetTenantsAsync();
    ValueTask<ITenantGrain?> CreateTenantAsync(string id, Dictionary<string, string?>? metadata = null);
    Task RemoveTenantAsync(string id);
}