using Flownodes.Sdk;

namespace Flownodes.Shared.Resourcing;

public interface IResourceGroupGrain : IResourceGrain
{
    Task RegisterResourceAsync(string name);
    Task UnregisterResourceAsync(string name);
    ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string name) where TResourceGrain : IResourceGrain;
    ValueTask<IResourceGrain?> GetResourceAsync(string name);
    ValueTask<HashSet<FlownodesId>> GetRegistrations();
    Task ClearRegistrationsAsync();
}