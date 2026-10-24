using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Domain.Namespaces;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Namespaces.ApplyNamespace;

/// <summary>
/// Records a Namespace the Registry announced, unless the message is not newer than the last one applied.
/// </summary>
/// <param name="Name">The name of the Namespace.</param>
/// <param name="Uid">The identifier of this incarnation of the Namespace.</param>
/// <param name="Seq">The sequence number of the message.</param>
public sealed record Command(string Name, Guid Uid, long Seq) : IRequest<Result>;

internal sealed class Handler(IIdentityDbContext database) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var known = await database.KnownNamespaces.SingleOrDefaultAsync(
            candidate => candidate.Name == request.Name,
            cancellationToken);

        if (known is null)
        {
            database.KnownNamespaces.Add(KnownNamespace.Create(request.Name, request.Uid, request.Seq));
        }
        else if (!known.Apply(request.Uid, request.Seq))
        {
            return Result.Found();
        }

        await database.SaveChangesAsync(cancellationToken);

        return Result.Updated();
    }
}
