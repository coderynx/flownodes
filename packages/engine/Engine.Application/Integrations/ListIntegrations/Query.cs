using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Engine.Domain.Integrations;

namespace Engine.Application.Integrations.ListIntegrations;

/// <summary>
/// Lists the Integrations installed with the deployment, including those that failed to load.
/// </summary>
public sealed record Query : IRequest<Result<IReadOnlyList<InstalledIntegration>>>;

internal sealed class Handler(IInstalledIntegrations installed)
    : IRequestHandler<Query, Result<IReadOnlyList<InstalledIntegration>>>
{
    public Task<Result<IReadOnlyList<InstalledIntegration>>> HandleAsync(
        Query request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Found(installed.All));
}
