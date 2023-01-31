using Flownodes.Shared.Interfaces;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
public sealed class AlertManagerPersistence
{
    [Id(0)] public List<Alert> Registrations { get; set; } = new();
    [Id(1)] public List<string> DriversNames { get; set; } = new();
}