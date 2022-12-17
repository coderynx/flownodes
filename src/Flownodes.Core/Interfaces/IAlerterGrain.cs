using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

public interface IAlerterGrain : IGrainWithStringKey
{
    Task<Alert> ProduceInfoAlertAsync(string frn, string message);
    Task<Alert> ProduceWarningAlertAsync(string frn, string message);
    Task<Alert> ProduceErrorAlertAsync(string frn, string message);
    Task ConfigureAsync(params string[] alertDriverIds);
    Task ClearAlertsAsync();
    Task<IEnumerable<Alert>> GetAlerts();
}