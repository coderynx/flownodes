using System.Text.Json;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Domain.Resources;

public sealed class SpecDiffTests
{
    [Fact]
    public void Between_ShouldFindNoChange_WhenOnlyKeyOrderAndNumberFormatDiffer()
    {
        // Arrange
        var before = Json("""{"a": 1, "b": {"c": [1, 2], "d": "x"}}""");
        var after = Json("""{"b": {"d": "x", "c": [1.0, 2]}, "a": 1.00}""");

        // Act
        var changes = SpecDiff.Between(before, after);

        // Assert
        changes.ShouldBeEmpty();
    }

    [Fact]
    public void Between_ShouldListEveryChangedLeafWithItsPath_WhenSpecsDiffer()
    {
        // Arrange
        var before = Json("""{"keep": true, "gone": 1, "nested": {"value": "old"}, "list": [1, 2, 3]}""");
        var after = Json("""{"keep": true, "added": "new", "nested": {"value": "new"}, "list": [1, 5]}""");

        // Act
        var changes = SpecDiff.Between(before, after);

        // Assert
        changes.Select(change => (change.Path, change.Kind))
            .ShouldBe(
            [
                ("spec.gone", SpecChangeKind.Removed),
                ("spec.nested.value", SpecChangeKind.Changed),
                ("spec.list[1]", SpecChangeKind.Changed),
                ("spec.list[2]", SpecChangeKind.Removed),
                ("spec.added", SpecChangeKind.Added)
            ]);
        var changed = changes.Single(change => change.Path == "spec.nested.value");
        changed.From!.Value.GetString().ShouldBe("old");
        changed.To!.Value.GetString().ShouldBe("new");
    }

    [Fact]
    public void Between_ShouldListEveryTopLevelFieldAsAdded_WhenThereIsNoPreviousSpec()
    {
        // Arrange
        var after = Json("""{"a": 1, "b": {"c": 2}}""");

        // Act
        var changes = SpecDiff.Between(null, after);

        // Assert
        changes.Select(change => (change.Path, change.Kind))
            .ShouldBe([("spec.a", SpecChangeKind.Added), ("spec.b", SpecChangeKind.Added)]);
    }

    [Fact]
    public void Between_ShouldReplaceTheWholeValue_WhenItsTypeChanges()
    {
        // Arrange
        var before = Json("""{"a": {"b": 1}}""");
        var after = Json("""{"a": [1]}""");

        // Act
        var changes = SpecDiff.Between(before, after);

        // Assert
        changes.Select(change => (change.Path, change.Kind)).ShouldBe([("spec.a", SpecChangeKind.Changed)]);
    }

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
