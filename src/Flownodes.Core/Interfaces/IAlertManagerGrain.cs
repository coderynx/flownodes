namespace Flownodes.Core.Interfaces;

public interface IAlertManagerGrain : IGrainWithStringKey
{
    ValueTask<Alert> FireInfoAsync(string targetResourceId, string description);
    ValueTask<Alert> FireWarningAsync(string targetResourceId, string description);
    ValueTask<Alert> FireErrorAsync(string targetResourceId, string description);
    Task SetupAsync(params string[] alertDriverIds);
    Task ClearAlertsAsync();
    ValueTask<IEnumerable<Alert>> GetAlerts();
}