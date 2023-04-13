using System.Collections.ObjectModel;
using Flownodes.Shared.Entities;

namespace Flownodes.Shared.Resourcing.Grains;

public interface IResourceManagerGrain : IEntityGrain
{
    ValueTask<bool> IsResourceRegistered(string name);

    ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string name)
        where TResourceGrain : IResourceGrain;

    ValueTask<ReadOnlyCollection<IResourceSummary>> GetAllResourceSummaries();

    ValueTask<TResourceGrain> DeployResourceAsync<TResourceGrain>(string name) where TResourceGrain : IResourceGrain;

    Task RemoveResourceAsync(string name);
    Task RemoveAllResourcesAsync();
    ValueTask<IResourceSummary?> GetResourceSummary(string name);
    ValueTask<IResourceGrain?> GetResourceAsync(string name);
    ValueTask<IReadOnlyList<IResourceGrain>> SearchResourcesByTags(HashSet<string> tags);
}