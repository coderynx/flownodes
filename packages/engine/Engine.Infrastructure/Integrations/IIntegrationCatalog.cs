using Flownodes.Sdk;

namespace Engine.Infrastructure.Integrations;

internal sealed record LoadedIntegration(IIntegration Integration, IServiceProvider Services);

internal interface IIntegrationCatalog
{
    LoadedIntegration? Find(string id);
}
