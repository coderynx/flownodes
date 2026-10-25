using System.Text.Json;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Domain.Resources;

public sealed class ResourceTests
{
    [Fact]
    public void Create_ShouldStartAtGenerationOneWithANewUid()
    {
        // Act
        var resource = Resource.Create(
            NamespaceName.Create("acme"),
            "Widget",
            ResourceName.Create("w-1"),
            Json("{}"),
            4);

        // Assert
        resource.Namespace.Value.ShouldBe("acme");
        resource.Kind.ShouldBe("Widget");
        resource.Name.Value.ShouldBe("w-1");
        resource.Generation.ShouldBe(1);
        resource.Seq.ShouldBe(4);
        resource.Uid.ShouldNotBe(Guid.Empty);
        resource.Status.ShouldBeNull();
    }

    [Fact]
    public void Apply_ShouldReplaceSpecAndBumpGeneration_WhenSpecChanges()
    {
        // Arrange
        var resource = Widget("""{"size": 1}""");

        // Act
        resource.Apply(Json("""{"colour": "red"}"""), 9);

        // Assert
        resource.Generation.ShouldBe(2);
        resource.Seq.ShouldBe(9);
        resource.Spec.GetProperty("colour").GetString().ShouldBe("red");
        resource.Spec.TryGetProperty("size", out _).ShouldBeFalse();
    }

    [Fact]
    public void Apply_ShouldThrowInvalidOperationException_WhenSpecMeansTheSame()
    {
        // Arrange
        var resource = Widget("""{"size": 1, "colour": "red"}""");

        // Act
        var act = () => resource.Apply(Json("""{"colour": "red", "size": 1.0}"""), 9);

        // Assert
        act.ShouldThrow<InvalidOperationException>();
        resource.Generation.ShouldBe(1);
    }

    [Theory]
    [InlineData("""{"colour": "red", "size": 1}""", true)]
    [InlineData("""{"size": 2}""", false)]
    public void Matches_ShouldTellWhetherASpecMeansTheSame(string spec, bool expected)
    {
        // Arrange
        var resource = Widget("""{"size": 1, "colour": "red"}""");

        // Act
        var matches = resource.Matches(Json(spec));

        // Assert
        matches.ShouldBe(expected);
    }

    [Fact]
    public void Delete_ShouldAdvanceSeqToTheDeletionMessage()
    {
        // Arrange
        var resource = Widget("{}");

        // Act
        resource.Delete(12);

        // Assert
        resource.Seq.ShouldBe(12);
    }

    [Fact]
    public void ReportStatus_ShouldKeepStatus_WhenUidMatchesAndStatusSeqIsNewer()
    {
        // Arrange
        var resource = Widget("{}");

        // Act
        var kept = resource.ReportStatus(resource.Uid, 1, 1, Json("""{"conditions": []}"""));

        // Assert
        kept.ShouldBeTrue();
        resource.ObservedGeneration.ShouldBe(1);
        resource.StatusSeq.ShouldBe(1);
        resource.Status!.Value.GetProperty("conditions").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public void ReportStatus_ShouldIgnoreStatus_WhenStatusSeqIsNotNewer()
    {
        // Arrange
        var resource = Widget("{}");
        resource.ReportStatus(resource.Uid, 5, 1, Json("""{"a": 1}"""));

        // Act
        var kept = resource.ReportStatus(resource.Uid, 5, 1, Json("""{"a": 2}"""));

        // Assert
        kept.ShouldBeFalse();
        resource.Status!.Value.GetProperty("a").GetInt32().ShouldBe(1);
    }

    [Fact]
    public void ReportStatus_ShouldIgnoreStatus_WhenItBelongsToAnotherIncarnation()
    {
        // Arrange
        var resource = Widget("{}");

        // Act
        var kept = resource.ReportStatus(Guid.NewGuid(), 1, 1, Json("{}"));

        // Assert
        kept.ShouldBeFalse();
        resource.Status.ShouldBeNull();
        resource.ObservedGeneration.ShouldBeNull();
    }

    private static Resource Widget(string spec) =>
        Resource.Create(NamespaceName.Create("acme"), "Widget", ResourceName.Create("w-1"), Json(spec), 1);

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Theory]
    [InlineData("""{"size": 1.0}""", 1)]
    [InlineData("""{"size": 2}""", 2)]
    public void GenerationAfter_ShouldTellTheGenerationApplyingASpecWouldReach(string spec, long expected)
    {
        // Arrange
        var resource = Widget("""{"size": 1}""");

        // Act
        var generation = resource.GenerationAfter(Json(spec));

        // Assert
        generation.ShouldBe(expected);
    }
}
