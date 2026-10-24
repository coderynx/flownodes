using Identity.Domain.Namespaces;
using Xunit;

namespace Identity.UnitTests.Domain.Namespaces;

public sealed class KnownNamespaceTests
{
    private static readonly Guid Uid = Guid.NewGuid();

    [Fact]
    public void Create_ShouldRecordEveryValue_WhenGivenThem()
    {
        // Act
        var known = KnownNamespace.Create("acme", Uid, 4);

        // Assert
        known.Name.ShouldBe("acme");
        known.Uid.ShouldBe(Uid);
        known.Seq.ShouldBe(4);
    }

    [Fact]
    public void Apply_ShouldAdvanceTheSeq_WhenTheMessageIsNewer()
    {
        // Arrange
        var known = KnownNamespace.Create("acme", Uid, 4);

        // Act
        var applied = known.Apply(Uid, 5);

        // Assert
        applied.ShouldBeTrue();
        known.Uid.ShouldBe(Uid);
        known.Seq.ShouldBe(5);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(3)]
    public void Apply_ShouldChangeNothing_WhenTheSeqIsNotNewer(long seq)
    {
        // Arrange
        var known = KnownNamespace.Create("acme", Uid, 4);

        // Act
        var applied = known.Apply(Uid, seq);

        // Assert
        applied.ShouldBeFalse();
        known.Seq.ShouldBe(4);
    }

    [Fact]
    public void Apply_ShouldTakeTheNewIncarnation_WhenTheUidDiffersWhateverTheSeq()
    {
        // Arrange
        var known = KnownNamespace.Create("acme", Uid, 4);
        var recreated = Guid.NewGuid();

        // Act
        var applied = known.Apply(recreated, 1);

        // Assert
        applied.ShouldBeTrue();
        known.Uid.ShouldBe(recreated);
        known.Seq.ShouldBe(1);
    }

    [Fact]
    public void IsDeletedBy_ShouldBeTrue_WhenTheMessageIsAboutThisIncarnationAndNewer()
    {
        // Arrange
        var known = KnownNamespace.Create("acme", Uid, 4);

        // Act
        var deleted = known.IsDeletedBy(Uid, 5);

        // Assert
        deleted.ShouldBeTrue();
    }

    [Theory]
    [InlineData(4)]
    [InlineData(3)]
    public void IsDeletedBy_ShouldBeFalse_WhenTheSeqIsNotNewer(long seq)
    {
        // Arrange
        var known = KnownNamespace.Create("acme", Uid, 4);

        // Act
        var deleted = known.IsDeletedBy(Uid, seq);

        // Assert
        deleted.ShouldBeFalse();
    }

    [Fact]
    public void IsDeletedBy_ShouldBeFalse_WhenTheMessageIsAboutAnotherIncarnation()
    {
        // Arrange
        var known = KnownNamespace.Create("acme", Uid, 4);

        // Act
        var deleted = known.IsDeletedBy(Guid.NewGuid(), 5);

        // Assert
        deleted.ShouldBeFalse();
    }
}
