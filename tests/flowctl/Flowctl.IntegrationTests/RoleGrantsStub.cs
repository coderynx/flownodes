using Coderynx.Functional.Results;
using Core.Domain.Authorization;
using Engine.Application.Authorization;

namespace Flowctl.IntegrationTests;

/// <summary>
/// Lets every caller through: these tests drive flowctl against the Engine's HTTP surface without a Silo, so the
/// Engine's replica of Role Grants is out of play. The Engine's own tests cover its authorisation.
/// </summary>
public sealed class RoleGrantsStub : IRoleGrants
{
    public Task<Result> RequireRoleAsync(
        Guid callerId,
        string @namespace,
        NamespaceRole role,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Updated());

    public Task<Result> RequireMemberAsync(Guid callerId, string @namespace, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Updated());

    public Task<Result> RequireAnyMembershipAsync(Guid callerId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Updated());
}
