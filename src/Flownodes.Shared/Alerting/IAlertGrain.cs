using Flownodes.Sdk.Alerting;

namespace Flownodes.Shared.Alerting;

public interface IAlertGrain : IEntityGrain
{
    Task InitializeAsync(string targetObjectName, DateTime firedAt, AlertSeverity severity,
        string description, ISet<string> drivers);

    Task FireAsync();

    Task ClearStateAsync();

    ValueTask<string> GetName();

    ValueTask<(string TargetObjectName, DateTime FiredAt, AlertSeverity Severity, string Description,
        ISet<string> Drivers)> GetState();
}