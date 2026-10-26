using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces.SeedDefaultNamespace;

/// <summary>
/// Creates the <c>default</c> Namespace when the Registry starts, if it does not exist yet. It acts for the Registry
/// itself, not for a caller.
/// </summary>
public sealed record Command : IRequest<Result<Namespace>>;

internal sealed class Handler(INamespaceCreation creation) : IRequestHandler<Command, Result<Namespace>>
{
    public async Task<Result<Namespace>> HandleAsync(Command request, CancellationToken cancellationToken = default) =>
        await creation.EnsureAsync(NamespaceName.Default, cancellationToken);
}
