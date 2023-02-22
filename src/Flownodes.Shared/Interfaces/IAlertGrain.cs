using Flownodes.Sdk.Alerting;

namespace Flownodes.Shared.Interfaces;

public interface IAlertGrain : IGrainWithStringKey
{
    Task InitializeAsync(string targetObjectName, DateTime firedAt, AlertSeverity severity,
        string description, ISet<string> alertDrivers);

    Task FireAsync();

    Task ClearStateAsync();
}