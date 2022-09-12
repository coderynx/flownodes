using Flownodes.Cluster.Core.Alerting;

namespace Flownodes.Cluster.Node.Models;

internal class AlerterPersistence
{
    public List<string> AlerterDrivers { get; set; } = new();
    public List<Alert> Alerts { get; set; } = new();
}