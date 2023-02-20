using System.Collections.ObjectModel;
using Flownodes.Shared.Models;

namespace Flownodes.Shared.Interfaces;

public interface IResourceManagerGrain : IGrainWithStringKey
{
    ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string tenantName, string resourceName)
        where TResourceGrain : IResourceGrain;

    ValueTask<ReadOnlyCollection<Resource>> GetAllResourceSummaries(string tenantName);

    ValueTask<TResourceGrain> DeployResourceAsync<TResourceGrain>(string tenantName, string resourceName,
        string behaviorId,
        Dictionary<string, object?> configuration = null,
        Dictionary<string, string?> metadata = null)
        where TResourceGrain : IResourceGrain;

    Task RemoveResourceAsync(string tenantName, string resourceName);
    Task RemoveAllResourcesAsync(string tenantName);
    ValueTask<Resource?> GetResourceSummary(string tenantName, string resourceName);
    ValueTask<IResourceGrain?> GetGenericResourceAsync(string tenantName, string resourceName);
}