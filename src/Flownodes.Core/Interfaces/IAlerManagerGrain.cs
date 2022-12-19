using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

public interface IAlerManagerGrain : IGrainWithStringKey
{
    ValueTask<Alert> ProduceInfoAlertAsync(string frn, string message);
    ValueTask<Alert> ProduceWarningAlertAsync(string frn, string message);
    ValueTask<Alert> ProduceErrorAlertAsync(string frn, string message);
    Task ConfigureAsync(params string[] alertDriverIds);
    Task ClearAlertsAsync();
    ValueTask<IEnumerable<Alert>> GetAlerts();
}