using Coderynx.Functional.Results.Errors;
using Coderynx.Functional.Results.Successes;
using Core.Application.Authorization;
using Core.Domain.Authorization;
using Registry.Application.Namespaces.ListNamespaces;
using Registry.Domain.Namespaces;
using Xunit;

namespace Registry.UnitTests.Namespaces.ListNamespaces;

public sealed class HandlerTests
{
    private const int DefaultLimit = 50;

    [Fact]
    public async Task HandleAsync_ShouldReturnInvalidInput_WhenSortKeyIsUnknown()
    {
        // Arrange
        var caller = Callers.Administrator();
        var handler = new Handler(TestDatabase.Create(Stored("acme"), users: [caller]));

        // Act
        var result = await handler.HandleAsync(ListQuery(caller.Id, sort: "uid"), Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.InvalidInput);
        result.Error.Code.ShouldBe("namespace.unknown_sort_key");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenCallerIsUnknown()
    {
        // Arrange
        var handler = new Handler(TestDatabase.Create(Stored("acme")));

        // Act
        var result = await handler.HandleAsync(ListQuery(Guid.NewGuid()), Ct);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.UnauthenticatedCode);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenCallerIsDisabled()
    {
        // Arrange
        var caller = Callers.DisabledAdministrator();
        var handler = new Handler(TestDatabase.Create(Stored("acme"), users: [caller]));

        // Act
        var result = await handler.HandleAsync(ListQuery(caller.Id), Ct);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.UnauthenticatedCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("name")]
    public async Task HandleAsync_ShouldListEveryNamespaceByName_WhenCallerIsAnAdministrator(string? sort)
    {
        // Arrange
        var caller = Callers.Administrator();
        var handler = new Handler(TestDatabase.Create(Stored("plant-7", "acme", "default"), users: [caller]));

        // Act
        var result = await handler.HandleAsync(ListQuery(caller.Id, sort: sort), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Found);
        Names(result.Value).ShouldBe(["acme", "default", "plant-7"]);
        result.Value.Total.ShouldBe(3);
    }

    [Fact]
    public async Task HandleAsync_ShouldListOnlyNamespacesWithARole_WhenCallerIsNotAnAdministrator()
    {
        // Arrange
        var caller = Callers.Member("plant-7");
        caller.GrantRole("acme", NamespaceRole.Engineer, 2);
        var handler = new Handler(TestDatabase.Create(Stored("plant-7", "acme", "default"), users: [caller]));

        // Act
        var result = await handler.HandleAsync(ListQuery(caller.Id), Ct);

        // Assert
        Names(result.Value).ShouldBe(["acme", "plant-7"]);
        result.Value.Total.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_ShouldListNothing_WhenCallerHoldsNoRole()
    {
        // Arrange
        var caller = UserAccess.Create(Guid.NewGuid());
        var handler = new Handler(TestDatabase.Create(Stored("acme", "default"), users: [caller]));

        // Act
        var result = await handler.HandleAsync(ListQuery(caller.Id), Ct);

        // Assert
        result.Value.Items.ShouldBeEmpty();
        result.Value.Total.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task HandleAsync_ShouldNotFilter_WhenQIsEmpty(string? q)
    {
        // Arrange
        var caller = Callers.Administrator();
        var handler = new Handler(TestDatabase.Create(Stored("acme", "default"), users: [caller]));

        // Act
        var result = await handler.HandleAsync(ListQuery(caller.Id, q), Ct);

        // Assert
        result.Value.Total.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepNamesContainingQ_WhenQIsGiven()
    {
        // Arrange
        var caller = Callers.Administrator();
        var handler = new Handler(TestDatabase.Create(Stored("plant-7", "plant-8", "acme"), users: [caller]));

        // Act
        var result = await handler.HandleAsync(ListQuery(caller.Id, "ant"), Ct);

        // Assert
        Names(result.Value).ShouldBe(["plant-7", "plant-8"]);
        result.Value.Total.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_ShouldMatchCaseSensitively_WhenQHasUppercase()
    {
        // Arrange
        var caller = Callers.Administrator();
        var handler = new Handler(TestDatabase.Create(Stored("acme"), users: [caller]));

        // Act
        var result = await handler.HandleAsync(ListQuery(caller.Id, "ACME"), Ct);

        // Assert
        result.Value.Total.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ShouldSortDescending_WhenDescIsSet()
    {
        // Arrange
        var caller = Callers.Administrator();
        var handler = new Handler(TestDatabase.Create(Stored("acme", "plant-7", "default"), users: [caller]));

        // Act
        var result = await handler.HandleAsync(ListQuery(caller.Id, desc: true), Ct);

        // Assert
        Names(result.Value).ShouldBe(["plant-7", "default", "acme"]);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnOnePageAndTotal_WhenOffsetAndLimitAreGiven()
    {
        // Arrange
        var caller = Callers.Administrator();
        var handler = new Handler(TestDatabase.Create(Stored("a", "b", "c", "d", "e"), users: [caller]));

        // Act
        var result = await handler.HandleAsync(ListQuery(caller.Id, offset: 1, limit: 2), Ct);

        // Assert
        Names(result.Value).ShouldBe(["b", "c"]);
        result.Value.Total.ShouldBe(5);
    }

    [Fact]
    public async Task HandleAsync_ShouldStartAtFirst_WhenOffsetIsNegative()
    {
        // Arrange
        var caller = Callers.Administrator();
        var handler = new Handler(TestDatabase.Create(Stored("a", "b", "c"), users: [caller]));

        // Act
        var result = await handler.HandleAsync(ListQuery(caller.Id, offset: -5, limit: 2), Ct);

        // Assert
        Names(result.Value).ShouldBe(["a", "b"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task HandleAsync_ShouldReturnOne_WhenLimitIsBelowOne(int limit)
    {
        // Arrange
        var caller = Callers.Administrator();
        var handler = new Handler(TestDatabase.Create(Stored("a", "b", "c"), users: [caller]));

        // Act
        var result = await handler.HandleAsync(ListQuery(caller.Id, limit: limit), Ct);

        // Assert
        Names(result.Value).ShouldBe(["a"]);
        result.Value.Total.ShouldBe(3);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTwoHundred_WhenLimitIsAboveTwoHundred()
    {
        // Arrange
        var caller = Callers.Administrator();
        var names = Enumerable.Range(0, 250).Select(index => $"ns-{index:D3}").ToArray();
        var handler = new Handler(TestDatabase.Create(Stored(names), users: [caller]));

        // Act
        var result = await handler.HandleAsync(ListQuery(caller.Id, limit: 1000), Ct);

        // Assert
        result.Value.Items.Count.ShouldBe(200);
        result.Value.Total.ShouldBe(250);
    }

    private static Query ListQuery(
        Guid callerId,
        string? q = null,
        string? sort = null,
        bool desc = false,
        int offset = 0,
        int limit = DefaultLimit) =>
        new(callerId, q, sort, desc, offset, limit);

    private static List<Namespace> Stored(params string[] names) =>
        names.Select(name => Namespace.Create(NamespaceName.Create(name))).ToList();

    private static string[] Names(NamespacePage page) =>
        page.Items.Select(@namespace => @namespace.Name.Value).ToArray();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
