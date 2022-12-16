namespace Flownodes.Core.Interfaces;

public interface IDataCollectorBehaviour
{
    Task<object?> UpdateAsync(string actionId, Dictionary<string, object?> parameters);
}