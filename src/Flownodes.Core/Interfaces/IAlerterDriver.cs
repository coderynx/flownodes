using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

[GenerateSerializer]
public sealed record Alert([property: Id(0)] Guid Id, [property: Id(1)] string TargetResourceId,
    [property: Id(2)] AlertSeverity Severity, [property: Id(3)] DateTime CreatedAt,
    [property: Id(4)] string Description);

[AttributeUsage(AttributeTargets.Class)]
public class AlerterDriverIdAttribute : Attribute
{
    public AlerterDriverIdAttribute(string id)
    {
        Id = id;
    }

    public string Id { get; set; }
}

public interface IAlerterDriver
{
    Task SendAlertAsync(Alert alert);
}