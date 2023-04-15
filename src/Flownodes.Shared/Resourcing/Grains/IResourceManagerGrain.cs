using System.Collections.ObjectModel;
using Flownodes.Shared.Entities;

namespace Flownodes.Shared.Resourcing.Grains;

public interface IResourceManagerGrain : IEntityGrain
{
    ValueTask<bool> IsResourceRegistered(string name);

    ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string name)
        where TResourceGrain : IResourceGrain;

    ValueTask<IEnumerable<ResourceSummary>> GetAllResourceSummaries();

    ValueTask<TResourceGrain> DeployResourceAsync<TResourceGrain>(string name) where TResourceGrain : IResourceGrain;

    Task RemoveResourceAsync(string name);
    Task RemoveAllResourcesAsync();
    ValueTask<IResourceGrain?> GetResourceAsync(string name);
    ValueTask<IEnumerable<IResourceGrain>> SearchResourcesByTags(HashSet<string> tags);
    ValueTask<IEnumerable<IResourceGrain>> GetResources();
    ValueTask<IEnumerable<IResourceGrain>> GetResources(string kind);
}