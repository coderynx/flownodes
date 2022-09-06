namespace Flownodes.Edge.Core.Resources;

public interface IDataCollectorBehavior
{
    Task<object?> UpdateAsync(string actionId, Dictionary<string, object?> parameters);
}