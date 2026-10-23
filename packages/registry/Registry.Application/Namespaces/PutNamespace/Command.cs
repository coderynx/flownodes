using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces.PutNamespace;

/// <summary>
/// Creates a Namespace, or finds it when it already exists.
/// </summary>
/// <param name="Name">The name of the Namespace, validated by the handler.</param>
public sealed record Command(string Name) : IRequest<Result<Namespace>>;

internal sealed class Handler(IApplicationDbContext database) : IRequestHandler<Command, Result<Namespace>>
{
    public async Task<Result<Namespace>> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        if (!NamespaceName.TryCreate(request.Name, out var name))
        {
            return Errors.InvalidName(request.Name);
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
