using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Application.Resources;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces.ResyncNamespaces;

/// <summary>
/// Publishes Namespaces again, each with every Resource in it, because the Engine asked for it (ADR-0014). Each
/// message carries the sequence number of the last change it describes, so the Engine drops what it already has. It
/// acts for the Engine, not for a caller.
/// </summary>
/// <param name="Name">The name of the one Namespace to publish, or <see langword="null"/> for every Namespace.</param>
public sealed record Command(string? Name) : IRequest<Result>;

internal sealed class Handler(IApplicationDbContext database) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var query = database.Namespaces.AsQueryable();
        if (request.Name is not null)
        {
            if (!NamespaceName.TryCreate(request.Name, out var name))
            {
                return Result.Found();
            }

            query = query.Where(@namespace => @namespace.Name == name);
        }

        var namespaces = await query.ToListAsync(cancellationToken);
        var names = namespaces.Select(@namespace => @namespace.Name).ToList();
        var resources = await database.Resources.Where(resource => names.Contains(resource.Namespace))
            .ToListAsync(cancellationToken);

        foreach (var @namespace in namespaces)
        {
            await database.PublishAsync(@namespace.ToApplied());

            foreach (var resource in resources.Where(resource => resource.Namespace == @namespace.Name))
            {
                await database.PublishAsync(resource.ToApplied());
            }
        }

        await database.SaveChangesAsync(cancellationToken);

        return Result.Found();
    }
}
