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

        var existing = await FindAsync(database, name, cancellationToken);
        if (existing is not null)
        {
            return Result.Found(existing);
        }

        var created = Namespace.Create(name);
        database.Namespaces.Add(created);

        try
        {
            await database.PublishAsync(created.ToApplied());
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            database.Namespaces.Remove(created);

            var winner = await FindAsync(database, name, cancellationToken);
            if (winner is null)
            {
                throw;
            }

            return Result.Found(winner);
        }

        return Result.Created(created);
    }

    private static Task<Namespace?> FindAsync(
        IApplicationDbContext database,
        NamespaceName name,
        CancellationToken cancellationToken) =>
        database.Namespaces.SingleOrDefaultAsync(@namespace => @namespace.Name == name, cancellationToken);
}
