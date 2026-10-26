using Coderynx.Functional.Results;
using Core.Application.Authorization;
using Core.Domain.Authorization;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal sealed class ResourceAccess(IAccessChecks access, KindCatalog catalog) : IResourceAccess
{
    public async Task<Result<UserAccess?>> AuthoriseChangesAsync(
        Guid callerId,
        ManifestReading reading,
        CancellationToken cancellationToken)
    {
        var scope = await access.ScopeAsync(callerId, cancellationToken);
        if (scope.IsFailure)
        {
            return scope.Error;
        }

        UserAccess? caller = null;
        if (reading.TouchesNamespaces)
        {
            var administrator = await access.RequireAdministratorAsync(callerId, cancellationToken);
            if (administrator.IsFailure)
            {
                return administrator.Error;
            }

            caller = administrator.Value;
        }

        foreach (var @namespace in reading.Namespaces)
        {
            var engineer = await access.RequireRoleAsync(
                callerId,
                @namespace,
                NamespaceRole.Engineer,
                cancellationToken);
            if (engineer.IsFailure)
            {
                return engineer.Error;
            }

            caller = engineer.Value;
        }

        return Result.Found(caller);
    }

    public async Task<Result<(IResourceKind Kind, NamespaceName Namespace)>> AuthoriseReadAsync(
        Guid callerId,
        string @namespace,
        string kinds,
        CancellationToken cancellationToken)
    {
        var caller = await access.RequireMemberAsync(callerId, @namespace, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        var kind = catalog.FindByRoute(kinds);
        if (kind is null)
        {
            return Errors.KindNotFound(kinds);
        }

        return NamespaceName.TryCreate(@namespace, out var name)
            ? Result.Found((kind, name))
            : Errors.NamespaceNotFound(@namespace);
    }
}
