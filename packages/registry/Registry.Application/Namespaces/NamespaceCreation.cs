using Coderynx.Functional.Results;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces;

internal static class NamespaceCreation
{
    public static async Task<Result<Namespace>> EnsureAsync(
        IApplicationDbContext database,
        string rawName,
        CancellationToken cancellationToken)
    {
        if (!NamespaceName.TryCreate(rawName, out var name))
        {
            return Errors.InvalidName(rawName);
        }

        var existing = await database.Namespaces.SingleOrDefaultAsync(
            @namespace => @namespace.Name == name,
            cancellationToken);
        if (existing is not null)
        {
            return Result.Found(existing);
        }

        var created = Namespace.Create(name);
        database.Namespaces.Add(created);

        await database.PublishAsync(created.ToApplied());
        await database.SaveChangesAsync(cancellationToken);

        return Result.Created(created);
    }
}
