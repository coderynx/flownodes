using System.Text.Json;
using System.Text.Json.Nodes;

namespace Flownodes.Shared.Resourcing.Grains;

[GenerateSerializer]
public sealed record DataSourceResult
{
    public DataSourceResult(object? inputObj)
    {
        JsonString = JsonSerializer.Serialize(inputObj);
    }

    [Id(0)] public string JsonString { get; }

    public object? ToObject()
    {
        return JsonSerializer.Deserialize<object>(JsonString);
    }

    public JsonNode? ToJsoNode()
    {
        return JsonNode.Parse(JsonString);
    }
}

public interface IDataSourceGrain : IConfigurableResourceGrain
{
    /// <summary>
    ///     Updates the BehaviourId.
    /// </summary>
    /// <param name="behaviourId">The BehaviourId to set.</param>
    Task UpdateBehaviourId(string behaviourId);
    ValueTask<DataSourceResult> GetDataAsync(string actionId, Dictionary<string, object?>? parameters = null);
}