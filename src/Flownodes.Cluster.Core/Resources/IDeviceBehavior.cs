namespace Flownodes.Cluster.Core.Resources;

public interface IDeviceBehavior
{
    // TODO: Encapsulate result in a class.
    Task<Dictionary<string, object?>> PerformAction(string actionId, Dictionary<string, object?>? parameters = null);
}