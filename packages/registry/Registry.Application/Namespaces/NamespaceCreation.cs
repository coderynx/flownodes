using Coderynx.Functional.Results;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces;

internal sealed class NamespaceCreation(IApplicationDbContext database) : INamespaceCreation
{
    public async Task<Result<Namespace>> EnsureAsync(string rawName, CancellationToken cancellationToken)
    {
        if (!NamespaceName.TryCreate(rawName, out var name))
        {
            return Errors.InvalidName(rawName);
        }

        var existing = await FindAsync(name, cancellationToken);
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

            var winner = await FindAsync(name, cancellationToken);
            if (winner is null)
            {
                throw;
            }

            return Result.Found(winner);
        }

        return Result.Created(created);
    }

    private Task<Namespace?> FindAsync(NamespaceName name, CancellationToken cancellationToken) =>
        database.Namespaces.SingleOrDefaultAsync(@namespace => @namespace.Name == name, cancellationToken);
}
