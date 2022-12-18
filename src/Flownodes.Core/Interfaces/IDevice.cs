using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

public record ActionRequest(string Id, Dictionary<string, object?>? Parameters = null);

public record ResourceContext(ResourceConfiguration? Configuration, Dictionary<string, string> Metadata,
    ResourceState? State);

[AttributeUsage(AttributeTargets.Class)]
public class DeviceIdAttribute : Attribute
{
    public DeviceIdAttribute(string id)
    {
        Id = id;
    }

    public string Id { get; init; }
}

[AttributeUsage(AttributeTargets.Class)]
public class DeviceDescriptionAttribute : Attribute
{
    public DeviceDescriptionAttribute(string description)
    {
        Description = description;
    }

    public string Description { get; init; }
}

public interface IDevice : IBehaviour
{
    Task OnStateChangeAsync(Dictionary<string, object?> newState, ResourceContext context);
}

public interface IBehaviour
{
    Task OnSetupAsync(ResourceContext context);
}