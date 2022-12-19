using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

public interface IAlerManagerGrain : IGrainWithStringKey
{
    ValueTask<Alert> AlertInfoAsync(string frn, string message);
    ValueTask<Alert> AlertWarningAsync(string frn, string message);
    ValueTask<Alert> AlertErrorAsync(string frn, string message);
    Task ConfigureAsync(params string[] alertDriverIds);
    Task ClearAlertsAsync();
    ValueTask<IEnumerable<Alert>> GetAlerts();
}