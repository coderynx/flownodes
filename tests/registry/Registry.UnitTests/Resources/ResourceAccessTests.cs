using Coderynx.Functional.Results;
using Core.Application.Authorization;
using Core.Domain.Authorization;
using Registry.Application.Resources;
using Xunit;

namespace Registry.UnitTests.Resources;

public sealed class ResourceAccessTests
{
    private readonly IAccessChecks _checks = Substitute.For<IAccessChecks>();

    [Fact]
    public async Task AuthoriseChangesAsync_ShouldReturnTheError_WhenTheCallerIsUnknown()
    {
        // Arrange
        var callerId = Guid.NewGuid();
        _checks.ScopeAsync(callerId, Ct).Returns(Errors.NamespaceNotFound("acme"));

        // Act
        var result = await Access().AuthoriseChangesAsync(callerId, Reading(["acme"]), Ct);

        // Assert
        result.IsFailure.ShouldBeTrue();
        await _checks.DidNotReceiveWithAnyArgs()
            .RequireRoleAsync(Guid.Empty, "", NamespaceRole.Engineer, CancellationToken.None);
    }

    [Fact]
    public async Task
        AuthoriseChangesAsync_ShouldRequireTheEngineerRoleInEveryNamespace_WhenTheReadingTouchesResources()
    {
        // Arrange
        var caller = Callers.Member("acme", NamespaceRole.Engineer);
        Allow(caller);
        _checks.RequireRoleAsync(caller.Id, "acme", NamespaceRole.Engineer, Ct).Returns(Result.Found(caller));
        _checks.RequireRoleAsync(caller.Id, "globex", NamespaceRole.Engineer, Ct)
            .Returns(Errors.NamespaceNotFound("globex"));

        // Act
        var allowed = await Access().AuthoriseChangesAsync(caller.Id, Reading(["acme"]), Ct);
        var refused = await Access().AuthoriseChangesAsync(caller.Id, Reading(["acme", "globex"]), Ct);

        // Assert
        allowed.Value.ShouldBe(caller);
        refused.Error.Code.ShouldBe("namespace.not_found");
    }

    [Fact]
    public async Task AuthoriseChangesAsync_ShouldRequireAnAdministrator_WhenTheReadingTouchesNamespaces()
    {
        // Arrange
        var caller = Callers.Member("acme", NamespaceRole.Engineer);
        Allow(caller);
        _checks.RequireAdministratorAsync(caller.Id, Ct).Returns(Errors.NamespaceNotFound("acme"));

        // Act
        var result = await Access().AuthoriseChangesAsync(caller.Id, Reading([], true), Ct);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task AuthoriseChangesAsync_ShouldReturnTheAdministrator_WhenOnlyNamespacesAreTouched()
    {
        // Arrange
        var caller = Callers.Administrator();
        Allow(caller);
        _checks.RequireAdministratorAsync(caller.Id, Ct).Returns(Result.Found(caller));

        // Act
        var result = await Access().AuthoriseChangesAsync(caller.Id, Reading([], true), Ct);

        // Assert
        result.Value.ShouldBe(caller);
    }

    [Fact]
    public async Task AuthoriseReadAsync_ShouldReturnTheKindAndNamespace_WhenTheCallerIsAMember()
    {
        // Arrange
        var caller = Callers.Member("acme");
        _checks.RequireMemberAsync(caller.Id, "acme", Ct).Returns(Result.Found(caller));

        // Act
        var result = await Access().AuthoriseReadAsync(caller.Id, "acme", "widgets", Ct);

        // Assert
        result.Value.Kind.Name.ShouldBe(WidgetKind.Kind);
        result.Value.Namespace.Value.ShouldBe("acme");
    }

    [Fact]
    public async Task AuthoriseReadAsync_ShouldReturnTheError_WhenTheCallerIsNotAMember()
    {
        // Arrange
        var callerId = Guid.NewGuid();
        _checks.RequireMemberAsync(callerId, "acme", Ct).Returns(Errors.NamespaceNotFound("acme"));

        // Act
        var result = await Access().AuthoriseReadAsync(callerId, "acme", "widgets", Ct);

        // Assert
        result.Error.Code.ShouldBe("namespace.not_found");
    }

    [Fact]
    public async Task AuthoriseReadAsync_ShouldReturnKindNotFound_WhenTheKindIsUnknown()
    {
        // Arrange
        var caller = Callers.Member("acme");
        _checks.RequireMemberAsync(caller.Id, "acme", Ct).Returns(Result.Found(caller));

        // Act
        var result = await Access().AuthoriseReadAsync(caller.Id, "acme", "gizmos", Ct);

        // Assert
        result.Error.Code.ShouldBe("kind.not_found");
    }

    private ResourceAccess Access() => new(_checks, WidgetKind.Catalog);

    private void Allow(UserAccess caller) =>
        _checks.ScopeAsync(caller.Id, Ct).Returns(Result.Found(new NamespaceScope(true, new HashSet<string>())));

    private static ManifestReading Reading(IReadOnlyList<string> namespaces, bool touchesNamespaces = false) =>
        new([], [], namespaces, touchesNamespaces);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
