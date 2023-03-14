namespace Flownodes.Sdk.Resourcing;

public interface IDataSourceBehaviour : IBehaviour
{
    ValueTask<object?> GetDataAsync(string actionId, Dictionary<string, object?>? parameters = null);
}