namespace Core.Application.Integrations;

/// <summary>
/// The catalogue of a host that loads no Integrations.
/// </summary>
public sealed class NoIntegrations : IIntegrationSchemas
{
    /// <inheritdoc />
    public IntegrationSchema? Find(string id) => null;
}
