using System.Text.Json;
using System.Text.Json.Nodes;

namespace Flownodes.Shared.Interfaces;

[GenerateSerializer]
public record DataSourceResult
{
    public DataSourceResult(object inputObj)
    {
        JsonString = JsonSerializer.Serialize(inputObj);
    }

    [Id(0)] public string JsonString { get; init; }

    public object? ToObject()
    {
        return JsonSerializer.Deserialize<object>(JsonString);
    }

    public JsonNode? ToJsoNode()
    {
        return JsonNode.Parse(JsonString);
    }
}

public interface IDataSourceGrain : IResourceGrain
{
    ValueTask<DataSourceResult> GetData(string actionId, Dictionary<string, object?>? parameters = null);
}