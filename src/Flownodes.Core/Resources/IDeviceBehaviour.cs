namespace Flownodes.Core.Resources;

public class BehaviourIdAttribute : Attribute
{
    public BehaviourIdAttribute(string id)
    {
        Id = id;
    }

    public string Id { get; set; }
}

public interface IDeviceBehaviour
{
    // TODO: Encapsulate result in a class.
    Task<Dictionary<string, object?>> PerformAction(string actionId, Dictionary<string, object?>? parameters = null,
        ResourceConfiguration? resourceConfiguration = null, ResourceState? resourceState = null);
}