using Coderynx.Functional.Results;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces;

internal interface INamespaceCreation
{
    Task<Result<Namespace>> EnsureAsync(string rawName, CancellationToken cancellationToken);
}
