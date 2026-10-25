using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Application.Resources.ReportStatus;

/// <summary>
/// Stores a Status the Engine reported beside its Resource (ADR-0014), unless it is about another incarnation or is
/// not newer than the one stored. It acts for the Engine, not for a caller.
/// </summary>
/// <param name="Namespace">The Namespace of the Resource.</param>
/// <param name="Kind">The Kind of the Resource.</param>
/// <param name="Name">The name of the Resource.</param>
/// <param name="Uid">The incarnation the Status is about.</param>
/// <param name="ObservedGeneration">The Generation the Engine has picked up.</param>
/// <param name="StatusSeq">The Engine's sequence number for the Status.</param>
/// <param name="Status">The Status, without its <c>observedGeneration</c>.</param>
public sealed record Command(
    string Namespace,
    string Kind,
    string Name,
    Guid Uid,
    long ObservedGeneration,
    long StatusSeq,
    JsonElement Status) : IRequest<Result>;

internal sealed class Handler(IApplicationDbContext database) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        if (!NamespaceName.TryCreate(request.Namespace, out var @namespace) ||
            !ResourceName.TryCreate(request.Name, out var name))
        {
            return Result.Found();
        }

        var resource = await database.Resources.SingleOrDefaultAsync(
            candidate => candidate.Namespace == @namespace && candidate.Kind == request.Kind && candidate.Name == name,
            cancellationToken);
        if (resource is null || !resource.ReportStatus(
                request.Uid,
                request.StatusSeq,
                request.ObservedGeneration,
                request.Status))
        {
            return Result.Found();
        }

        await database.SaveChangesAsync(cancellationToken);

        return Result.Updated();
    }
}
