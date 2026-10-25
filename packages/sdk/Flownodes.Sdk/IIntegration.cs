using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Flownodes.Sdk;

/// <summary>
/// What an Integration declares about itself to the Silo that loads it.
/// </summary>
public interface IIntegration
{
    /// <summary>
    /// Gets the vendor-qualified id Connections refer to, such as <c>flownodes.mqtt</c>: lowercase dot-separated
    /// segments of letters, digits and hyphens, at least two.
    /// </summary>
    string Id { get; }
    /// <summary>
    /// Gets the shape of a Connection's <c>config</c>, which the Registry checks when a Connection is applied.
    /// </summary>
    ConfigSchema ConnectionConfig { get; }
    /// <summary>
    /// Gets the Device Types this Integration defines.
    /// </summary>
    IReadOnlyList<DeviceType> DeviceTypes { get; }

    /// <summary>
    /// Registers the Integration's own services in its private container.
    /// </summary>
    /// <remarks>
    /// The container already holds a logger, <see cref="TimeProvider"/> and <see cref="IHttpClientFactory"/> from the
    /// host and nothing else of the host's.
    /// </remarks>
    /// <param name="services">The Integration's own service collection.</param>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Creates a Connection that has not started.
    /// </summary>
    /// <param name="services">The Integration's private container.</param>
    /// <param name="config">The Connection's <c>config</c>, which passed <see cref="ConnectionConfig"/>.</param>
    /// <param name="sink">Where the Connection pushes what it learns.</param>
    /// <returns>The Connection.</returns>
    IConnection CreateConnection(IServiceProvider services, JsonElement config, IConnectionSink sink);
}
