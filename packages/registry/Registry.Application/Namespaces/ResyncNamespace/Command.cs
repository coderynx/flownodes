using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Application.Resources;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces.ResyncNamespace;

/// <summary>
/// Publishes a Namespace again, with every Resource in it, because the Engine asked for it (ADR-0014). Each message
/// carries the sequence number of the last change it describes, so the Engine drops what it already has. It acts for
/// the Engine, not for a caller.
/// </summary>
/// <param name="Name">The name of the Namespace to publish.</param>
public sealed record Command(string Name) : IRequest<Result>;

internal sealed class Handler(IApplicationDbContext database) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        if (!NamespaceName.TryCreate(request.Name, out var name))
        {
            return Result.Found();
        }

        var @namespace = await database.Namespaces.FirstOrDefaultAsync(
            @namespace => @namespace.Name == name,
            cancellationToken);
        if (@namespace is null)
        {
            return Result.Found();
        }

        var resources = await database.Resources.Where(resource => resource.Namespace == name)
            .ToListAsync(cancellationToken);

        await database.PublishAsync(@namespace.ToApplied());
        foreach (var resource in resources)
        {
            await database.PublishAsync(resource.ToApplied());
        }

        await database.SaveChangesAsync(cancellationToken);

        return Result.Found();
    }
}
