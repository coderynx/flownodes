namespace Flownodes.Core.Resources;

public interface IDataCollectorBehaviour
{
    Task<object?> UpdateAsync(string actionId, Dictionary<string, object?> parameters);
}