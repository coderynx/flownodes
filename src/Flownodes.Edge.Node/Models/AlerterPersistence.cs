using Flownodes.Edge.Core.Alerting;

namespace Flownodes.Edge.Node.Models;

internal class AlerterPersistence
{
    public List<string> AlerterDrivers { get; set; } = new();
    public List<Alert> Alerts { get; set; } = new();
}