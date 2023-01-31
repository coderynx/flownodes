using Flownodes.Shared.Models;

namespace Flownodes.Shared.Interfaces;

[GenerateSerializer]
public sealed class Alert
{
    public Alert(Guid Id, string TargetResourceId, AlertSeverity Severity, DateTime CreatedAt, string Description)
    {
        this.Id = Id;
        this.TargetResourceId = TargetResourceId;
        this.Severity = Severity;
        this.CreatedAt = CreatedAt;
        this.Description = Description;
    }

    [Id(0)] public Guid Id { get; init; }
    [Id(1)] public string TargetResourceId { get; init; }
    [Id(2)] public AlertSeverity Severity { get; init; }
    [Id(3)] public DateTime CreatedAt { get; init; }
    [Id(4)] public string Description { get; init; }
}