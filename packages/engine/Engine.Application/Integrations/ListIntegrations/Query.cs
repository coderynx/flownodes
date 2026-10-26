using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Engine.Application.Authorization;
using Engine.Domain.Integrations;

namespace Engine.Application.Integrations.ListIntegrations;

/// <summary>
/// Lists the Integrations installed with the deployment, including those that failed to load.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
public sealed record Query(Guid CallerId) : IRequest<Result<IReadOnlyList<InstalledIntegration>>>;

internal sealed class Handler(IRoleGrants grants, IInstalledIntegrations installed)
    : IRequestHandler<Query, Result<IReadOnlyList<InstalledIntegration>>>
{
    public async Task<Result<IReadOnlyList<InstalledIntegration>>> HandleAsync(
        Query request,
        CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireAnyMembershipAsync(request.CallerId, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return Result.Found(installed.All);
    }
}
