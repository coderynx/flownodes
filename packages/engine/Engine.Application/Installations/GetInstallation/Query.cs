using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Engine.Application.Authorization;
using Engine.Domain.Installations;

namespace Engine.Application.Installations.GetInstallation;

/// <summary>
/// Gets the Installation's identifier and the Flownodes version.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
public sealed record Query(Guid CallerId) : IRequest<Result<Installation>>;

internal sealed class Handler(IRoleGrants grants, IInstallationIdentity identity)
    : IRequestHandler<Query, Result<Installation>>
{
    public async Task<Result<Installation>> HandleAsync(Query request, CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireAnyMembershipAsync(request.CallerId, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return Result.Found(identity.Current);
    }
}
