using System.Text.Json;
using Flownodes.Sdk;
using Microsoft.Extensions.DependencyInjection;

[assembly: FlownodesIntegration(typeof(Flownodes.Simulator.SimulatorIntegration))]

namespace Flownodes.Simulator;

internal sealed class SimulatorIntegration : IIntegration
{
    private static readonly ConfigSchema Address = new([new ConfigField("id", ConfigFieldType.Integer, true)]);
    public string Id => "flownodes.simulator";
    public ConfigSchema ConnectionConfig { get; } = new([new ConfigField("units", ConfigFieldType.Integer)]);
    public IReadOnlyList<DeviceType> DeviceTypes { get; } =
    [
        new(
            SimulatedConnection.Thermometer,
            Address,
            ConfigSchema.Empty,
            (_, _) => new DeviceSurface(
                [new DataPointDefinition("temperature", DataPointType.Number, Unit: "°C", Min: -50, Max: 150)],
                [new ActionDefinition("set", true), new ActionDefinition("fail")])),
        new(
            SimulatedConnection.Switch,
            Address,
            ConfigSchema.Empty,
            (_, _) => new DeviceSurface(
                [new DataPointDefinition("on", DataPointType.Bool, true)],
                [new ActionDefinition("toggle")]))
    ];

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public IConnection CreateConnection(IServiceProvider services, JsonElement config, IConnectionSink sink) =>
        new SimulatedConnection(
            services.GetRequiredService<TimeProvider>(),
            config.TryGetProperty("units", out var units) ? units.GetInt32() : 0,
            sink);
}
