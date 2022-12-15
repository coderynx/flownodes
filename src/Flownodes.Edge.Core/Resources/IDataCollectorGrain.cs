namespace Flownodes.Edge.Core.Resources;

public interface IDataCollectorGrain : IGrainWithStringKey
{
    Task ConfigureAsync(string behaviorId, Dictionary<string, object?>? configuration = null,
        Dictionary<string, string>? metadata = null);

    Task<object> CollectAsync(string actionId, Dictionary<string, object?>? parameters = null);
    Task<string> GetFrn();
    Task SelfRemoveAsync();
}