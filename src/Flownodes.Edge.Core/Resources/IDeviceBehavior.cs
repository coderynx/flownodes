namespace Flownodes.Edge.Core.Resources;

public class BehaviorIdAttribute : Attribute
{
    public BehaviorIdAttribute(string id)
    {
        Id = id;
    }

    public string Id { get; set; }
}

public interface IDeviceBehavior
{
    // TODO: Encapsulate result in a class.
    Task<Dictionary<string, object?>> PerformAction(string actionId, Dictionary<string, object?>? parameters = null,
        ResourceConfiguration? resourceConfiguration = null, ResourceState? resourceState = null);
}