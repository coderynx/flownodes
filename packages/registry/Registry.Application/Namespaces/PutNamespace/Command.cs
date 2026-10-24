using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Application.Authorization;
using Registry.Application.Database;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces.PutNamespace;

/// <summary>
/// Creates a Namespace, or finds it when it already exists. Only an Administrator can.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Name">The name of the Namespace, validated by the handler.</param>
public sealed record Command(Guid CallerId, string Name) : IRequest<Result<Namespace>>;

internal sealed class Handler(IApplicationDbContext database) : IRequestHandler<Command, Result<Namespace>>
{
    public async Task<Result<Namespace>> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var caller = await database.RequireAdministratorAsync(request.CallerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        return await NamespaceCreation.EnsureAsync(database, request.Name, cancellationToken);
    }
}
