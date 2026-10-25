using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Domain.Resources;

public sealed class SecretKindTests
{
    private static readonly NamespaceSnapshot Empty = new(NamespaceName.Create("acme"), []);

    [Fact]
    public void Validate_ShouldAcceptTheSpec_WhenDataHoldsTextValues()
    {
        // Arrange
        var spec = Widgets.Json("""{"data": {"region": "eu-west", "db.token": "hunter2"}}""");

        // Act
        var errors = new SecretKind().Validate(spec, Empty);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{}""", "spec.data")]
    [InlineData("""{"data": {}}""", "spec.data")]
    [InlineData("""{"data": "x"}""", "spec.data")]
    [InlineData("""{"data": {"password": 1}}""", "spec.data.password")]
    [InlineData("""{"data": {"password": ""}}""", "spec.data.password")]
    [InlineData("""{"data": {"bad key": "x"}}""", "spec.data.bad key")]
    [InlineData("""{"data": {"a": "x"}, "extra": 1}""", "spec.extra")]
    public void Validate_ShouldReportThePath_WhenTheSpecIsInvalid(string json, string path)
    {
        // Act
        var errors = new SecretKind().Validate(Widgets.Json(json), Empty).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == path);
    }

    [Fact]
    public void Seal_ShouldReplaceEveryValueWithItsFingerprint()
    {
        // Arrange
        var spec = Widgets.Json("""{"data": {"token": "hunter2"}}""");

        // Act
        var sealedSpec = SecretKind.Seal(spec, value => $"fp:{value.Length}");

        // Assert
        sealedSpec.GetRawText().ShouldBe("""{"data":{"token":"fp:7"}}""");
    }

    [Fact]
    public void Values_ShouldReturnEachKeyWithItsValue()
    {
        // Act
        var values = SecretKind.Values(Widgets.Json("""{"data": {"a": "1", "b": "2"}}"""));

        // Assert
        values.ShouldBe(new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" });
    }

    [Fact]
    public void Redact_ShouldKeepOnlyTheKeyNamesInOrder_WhenTheSpecIsStored()
    {
        // Act
        var shown = SecretKind.Redact(Widgets.Json("""{"data": {"b": "fp-b", "a": "fp-a"}}"""));

        // Assert
        shown.GetRawText().ShouldBe("""{"keys":["a","b"]}""");
    }

    [Fact]
    public void Hide_ShouldReplaceTheValuesAndKeepThePaths_WhenChangesAreGiven()
    {
        // Arrange
        var changes = SpecDiff.Between(
            Widgets.Json("""{"data": {"a": "old", "gone": "x"}}"""),
            Widgets.Json("""{"data": {"a": "new", "added": "y"}}"""));

        // Act
        var hidden = SecretKind.Hide(changes);

        // Assert
        hidden.Select(change => (change.Path, change.Kind)).ShouldBe(changes.Select(c => (c.Path, c.Kind)));
        hidden.Single(change => change.Path == "spec.data.a").To!.Value.GetString().ShouldBe(SecretKind.Hidden);
        hidden.Single(change => change.Path == "spec.data.gone").To.ShouldBeNull();
        hidden.Single(change => change.Path == "spec.data.added").From.ShouldBeNull();
        hidden.ShouldAllBe(change => !change.From.HasValue || change.From.Value.GetString() == "(hidden)");
    }

    [Fact]
    public void References_ShouldBeEmpty()
    {
        // Act
        var references = new SecretKind().References(Widgets.Json("""{"data": {"a": "1"}}"""));

        // Assert
        references.ShouldBeEmpty();
    }
}
