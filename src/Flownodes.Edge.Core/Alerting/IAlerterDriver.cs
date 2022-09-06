namespace Flownodes.Edge.Core.Alerting;

public interface IAlerterDriver
{
    Task SendAlertAsync(Alert alert);
}