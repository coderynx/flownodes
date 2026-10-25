using Engine.Application.Integrations;
using Engine.Domain.Integrations;
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
    ILogger<InstalledIntegrations> logger) : IHostedService, IInstalledIntegrations
{
    private IReadOnlyList<InstalledIntegration> _all = [];
    public IReadOnlyList<InstalledIntegration> All => _all;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var directory = options.Value.Directory ?? Path.Combine(AppContext.BaseDirectory, "integrations");
        _all = [.. loader.Load(directory).Select(load => load.Installed)];

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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
