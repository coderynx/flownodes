using Coderynx.Functional.Results;
using Core.Domain.Authorization;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal interface IResourceAccess
{
    Task<Result<UserAccess?>> AuthoriseChangesAsync(
        Guid callerId,
        ManifestReading reading,
        CancellationToken cancellationToken);

    Task<Result<(IResourceKind Kind, NamespaceName Namespace)>> AuthoriseReadAsync(
        Guid callerId,
        string @namespace,
        string kinds,
        CancellationToken cancellationToken);
}
