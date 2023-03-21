namespace Flownodes.Sdk.Resourcing.Behaviours;

public interface IDataSourceBehaviour : IBehaviour
{
    ValueTask<object?> GetDataAsync(string actionId, Dictionary<string, object?>? parameters = null);
}