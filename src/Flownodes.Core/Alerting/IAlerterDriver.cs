namespace Flownodes.Core.Alerting;

public interface IAlerterDriver
{
    Task SendAlertAsync(Alert alert);
}