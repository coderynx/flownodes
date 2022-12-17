using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

public record BehaviourActionRequest(string Id, Dictionary<string, object?>? Parameters = null);

public record BehaviourResourceContext(ResourceConfiguration? Configuration, ResourceState? State);

[AttributeUsage(AttributeTargets.Class)]
public class BehaviourIdAttribute : Attribute
{
    public BehaviourIdAttribute(string id)
    {
        Id = id;
    }

    public string Id { get; init; }
}

public class BehaviourDescriptionAttribute : Attribute
{
    public BehaviourDescriptionAttribute(string description)
    {
        Description = description;
    }

    public string Description { get; init; }
}

public interface IDeviceBehaviour
{
    // TODO: Encapsulate result in a class.
    Task<Dictionary<string, object?>> PerformAction(BehaviourActionRequest request, BehaviourResourceContext context);
}