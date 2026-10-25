using Flownodes.Sdk;
using Microsoft.Extensions.DependencyInjection;

[assembly: FlownodesIntegration(typeof(Flownodes.Simulator.SimulatorIntegration))]

namespace Flownodes.Simulator;

internal sealed class SimulatorIntegration : IIntegration
{
    public string Id => "flownodes.simulator";
    public IReadOnlyList<DeviceType> DeviceTypes { get; } = [new("Thermometer"), new("Switch")];

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<SimulatedClock>();
    }
}
