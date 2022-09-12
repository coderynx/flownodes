namespace Flownodes.Cluster.Core.Alerting;

public interface IAlerterDriver
{
    Task SendAlertAsync(Alert alert);
}