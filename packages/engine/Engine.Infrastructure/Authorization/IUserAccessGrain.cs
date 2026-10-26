using Core.Domain.Authorization;

namespace Engine.Infrastructure.Authorization;

internal interface IUserAccessGrain : IGrainWithGuidKey
{
    Task GrantRoleAsync(string @namespace, NamespaceRole role, long seq);
    Task RevokeRoleAsync(string @namespace, NamespaceRole role, long seq);
    Task DisableAsync(long seq);
    Task EnableAsync(long seq);
    Task DeleteAsync(long seq);
    Task<UserAccessSnapshot> GetAsync();
}

[GenerateSerializer]
internal sealed record UserAccessSnapshot(
    [property: Id(0)] bool IsActive,
    [property: Id(1)] IReadOnlyList<GrantState> Grants);
