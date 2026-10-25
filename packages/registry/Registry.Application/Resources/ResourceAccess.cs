using Coderynx.Functional.Results;
using Core.Application.Authorization;
using Core.Domain.Authorization;
using Registry.Application.Database;

namespace Registry.Application.Resources;

internal static class ResourceAccess
{
    public static async Task<Result<UserAccess?>> AuthoriseChangesAsync(
        this IApplicationDbContext database,
        Guid callerId,
        ManifestReading reading,
        CancellationToken cancellationToken)
    {
        var scope = await database.ScopeAsync(callerId, cancellationToken);
        if (scope.IsFailure)
        {
            return scope.Error;
        }

        UserAccess? caller = null;
        if (reading.TouchesNamespaces)
        {
            var administrator = await database.RequireAdministratorAsync(callerId, cancellationToken);
            if (administrator.IsFailure)
            {
                return administrator.Error;
            }

            caller = administrator.Value;
        }

        foreach (var @namespace in reading.Namespaces)
        {
            var engineer = await database.RequireRoleAsync(
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
}
