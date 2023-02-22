using Flownodes.Sdk.Alerting;

namespace Flownodes.Shared.Interfaces;

public interface IAlertGrain : IGrainWithStringKey
{
    Task InitializeAsync(string targetObjectName, DateTime firedAt, AlertSeverity severity,
        string description, ISet<string> drivers);

    Task FireAsync();

    Task ClearStateAsync();

    ValueTask<(string TargetObjectName, DateTime FiredAt, AlertSeverity Severity, string Description,
        ISet<string> Drivers)> GetState();
}