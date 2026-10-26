using Registry.Application.Resources;

namespace Registry.Application.Secrets;

internal interface ISecretStorage
{
    Task StoreAsync(ManifestEntry entry, CancellationToken cancellationToken);
    Task ForgetAsync(ManifestEntry entry, CancellationToken cancellationToken);
}
