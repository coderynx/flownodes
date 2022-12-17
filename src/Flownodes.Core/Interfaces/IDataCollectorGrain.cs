using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

public interface IDataCollectorGrain : IGrainWithStringKey
{
    Task ConfigureAsync(string behaviorId, ResourceConfiguration configuration,
        Dictionary<string, string>? metadata = null);

    Task<object> CollectAsync(string actionId, Dictionary<string, object?>? parameters = null);
    Task<string> GetFrn();
    Task SelfRemoveAsync();
}