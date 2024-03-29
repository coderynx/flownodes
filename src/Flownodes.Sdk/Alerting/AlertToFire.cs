﻿using Flownodes.Sdk.Entities;

namespace Flownodes.Sdk.Alerting;

public enum AlertSeverity
{
    Verbose,
    Informational,
    Warning,
    Error,
    Critical
}

public record AlertToFire(EntityId Id, DateTime FiredAt, AlertSeverity Severity, string TargetResourceId,
    string Description);