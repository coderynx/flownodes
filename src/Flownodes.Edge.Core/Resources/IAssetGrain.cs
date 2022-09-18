using Newtonsoft.Json.Linq;
using Orleans;

namespace Flownodes.Edge.Core.Resources;

/// <summary>
///     The data object grain is a resource that allows to store custom data.
/// </summary>
public interface IAssetGrain : IGrainWithStringKey
{
    Task UpdateAsync(object data);
    Task<JToken?> QueryData(string jsonPath);
    Task<JObject> GetData();
    Task<string> GetFrn();
    Task SelfRemoveAsync();
}