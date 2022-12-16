using Flownodes.Core.Alerting;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
public class AlerterPersistence
{
    [Id(0)] public List<string> AlerterDrivers { get; set; } = new();
    [Id(1)] public List<Alert> Alerts { get; set; } = new();
}