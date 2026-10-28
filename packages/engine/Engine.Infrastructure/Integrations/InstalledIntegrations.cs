using System.Text.Json;
using Core.Application.Integrations;
using Engine.Application.Integrations;
using Engine.Domain.Integrations;
using Flownodes.Sdk;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Infrastructure.Integrations;

/// <summary>
/// Loads the deployment's Integrations when the Silo starts and keeps what it found.
/// </summary>
internal sealed class InstalledIntegrations(
    IntegrationLoader loader,
    IOptions<IntegrationOptions> options,
    ILogger<InstalledIntegrations> logger)
    : IHostedService, IInstalledIntegrations, IIntegrationCatalog, IIntegrationSchemas
{
    private IReadOnlyList<InstalledIntegration> _all = [];
    private IReadOnlyDictionary<string, LoadedIntegration> _loaded = new Dictionary<string, LoadedIntegration>();
    public IReadOnlyList<InstalledIntegration> All => _all;
    public LoadedIntegration? Find(string id) => _loaded.GetValueOrDefault(id);

    IntegrationSchema? IIntegrationSchemas.Find(string id) =>
        Find(id) is { } loaded
            ? new IntegrationSchema(
                config => Problems(loaded.Integration.ConnectionConfig, config),
                [
                    .. loaded.Integration.DeviceTypes.Select(deviceType => new DeviceTypeSchema(
                        deviceType.Name,
                        address => Problems(deviceType.Address, address),
                        config => Problems(deviceType.Config, config),
                        (address, config) => Schema(deviceType.Surface(address, config))))
                ])
            : null;

    private static DeviceSurfaceSchema Schema(DeviceSurface surface) =>
        new(
            [
                .. surface.DataPoints.Select(dataPoint => new DeviceDataPointSchema(
                    dataPoint.Name,
                    dataPoint.Type.ToString().ToLowerInvariant(),
                    dataPoint.Writable))
            ],
            [.. surface.Actions.Select(action => action.Name)],
            [.. surface.Events.Select(@event => @event.Name)]);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var directory = options.Value.Directory ?? Path.Combine(AppContext.BaseDirectory, "integrations");
        var loads = loader.Load(directory);
        _all = [.. loads.Select(load => load.Installed)];
        _loaded = loads.Where(load => load is { Integration: not null, Services: not null })
            .ToDictionary(
                load => load.Installed.Id!.Value,
                load => new LoadedIntegration(load.Integration!, load.Services!));

        foreach (var integration in _all)
        {
            if (integration.IsLoaded)
            {
                logger.LogInformation(
                    "Loaded Integration {Id} {Version} from {Folder}",
                    integration.Id,
                    integration.Version,
                    integration.Folder);
            }
            else
            {
                logger.LogWarning(
                    "Integration in {Folder} did not load ({Failure}): {Message}",
                    integration.Folder,
                    integration.Failure,
                    integration.Message);
            }
        }

        return Task.CompletedTask;
    }

    private static IReadOnlyList<SchemaProblem> Problems(ConfigSchema schema, JsonElement value) =>
        [.. schema.Validate(value).Select(problem => new SchemaProblem(problem.Path, problem.Message))];

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
