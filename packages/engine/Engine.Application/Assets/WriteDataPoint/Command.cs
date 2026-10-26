using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Domain.Authorization;
using Engine.Application.Authorization;

namespace Engine.Application.Assets.WriteDataPoint;

/// <summary>
/// Writes a writable Data Point of an Asset, writing through to the Device when it is mapped to one.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Asset.</param>
/// <param name="Asset">The name of the Asset.</param>
/// <param name="DataPoint">The name of the Data Point.</param>
/// <param name="Value">The value to write.</param>
public sealed record Command(Guid CallerId, string Namespace, string Asset, string DataPoint, JsonElement Value)
    : IRequest<Result>;

internal sealed class Handler(IRoleGrants grants, IAssetRuntime assets) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireRoleAsync(
            request.CallerId,
            request.Namespace,
            NamespaceRole.Operator,
            cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return await assets.WriteDataPointAsync(
            request.Namespace,
            request.Asset,
            request.DataPoint,
            request.Value,
            cancellationToken);
    }
}
