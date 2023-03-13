using System.Collections.ObjectModel;
using Flownodes.Shared.Models;

namespace Flownodes.Shared.Interfaces;

public interface IResourceManagerGrain : IGrainWithStringKey
{
    ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string tenantName, string resourceName)
        where TResourceGrain : IResourceGrain;

    ValueTask<ReadOnlyCollection<ResourceSummary>> GetAllResourceSummaries(string tenantName);

    ValueTask<TResourceGrain> DeployResourceAsync<TResourceGrain>(string tenantName, string resourceName,
        Dictionary<string, object?>? configuration = null,
        Dictionary<string, string?>? metadata = null)
        where TResourceGrain : IResourceGrain;

    Task RemoveResourceAsync(string tenantName, string resourceName);
    Task RemoveAllResourcesAsync(string tenantName);
    ValueTask<ResourceSummary?> GetResourceSummary(string tenantName, string resourceName);
    ValueTask<IResourceGrain?> GetGenericResourceAsync(string tenantName, string resourceName);
    ValueTask<IReadOnlyList<IResourceGrain>> SearchResourcesByTags(string tenantName, HashSet<string> tags);
}