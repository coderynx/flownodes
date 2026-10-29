using Registry.Domain.Namespaces;
using Xunit;

namespace Registry.UnitTests.Domain.Namespaces;

public sealed class NamespaceNameTests
{
    private const string LongestValid = "0123456789012345678901234567890123456789012345678901234567890ab";
    private const string TooLong = "01234567890123456789012345678901234567890123456789012345678901abc";

    [Theory]
    [InlineData("acme")]
    [InlineData("a")]
    [InlineData("7")]
    [InlineData("plant-7")]
    [InlineData("a--b")]
    [InlineData(NamespaceName.Default)]
    [InlineData(LongestValid)]
    public void TryCreate_ShouldReturnName_WhenRawIsADnsLabel(string raw)
    {
        // Act
        var valid = NamespaceName.TryCreate(raw, out var name);

        // Assert
        valid.ShouldBeTrue();
        name.ShouldNotBeNull().Value.ShouldBe(raw);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Acme")]
    [InlineData("under_score")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("-")]
    [InlineData("dotted.name")]
    [InlineData("white space")]
    [InlineData(" acme")]
    [InlineData("acme\n")]
    [InlineData(TooLong)]
    public void TryCreate_ShouldReturnFalse_WhenRawIsNotADnsLabel(string? raw)
    {
        // Act
        var valid = NamespaceName.TryCreate(raw, out var name);

        // Assert
        valid.ShouldBeFalse();
        name.ShouldBeNull();
    }

    [Fact]
    public void TryCreate_ShouldAcceptSixtyThreeCharacters_WhenOneMoreIsRejected()
    {
        // Act
        var longest = NamespaceName.TryCreate(LongestValid, out _);
        var longer = NamespaceName.TryCreate(LongestValid + "c", out _);

        // Assert
        LongestValid.Length.ShouldBe(63);
        longest.ShouldBeTrue();
        longer.ShouldBeFalse();
    }

    [Fact]
    public void Create_ShouldReturnName_WhenRawIsADnsLabel()
    {
        // Act
        var name = NamespaceName.Create("acme");

        // Assert
        name.Value.ShouldBe("acme");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenRawIsNotADnsLabel()
    {
        // Act
        var act = () => NamespaceName.Create("Acme");

        // Assert
        act.ShouldThrow<ArgumentException>().ParamName.ShouldBe("raw");
    }

    [Theory]
    [InlineData(NamespaceName.Default, true)]
    [InlineData("acme", false)]
    [InlineData("default-2", false)]
    public void IsDefault_ShouldBeTrue_WhenNameIsDefault(string raw, bool expected)
    {
        // Arrange
        var name = NamespaceName.Create(raw);

        // Act
        var isDefault = name.IsDefault;

        // Assert
        isDefault.ShouldBe(expected);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var name = NamespaceName.Create("acme");

        // Act
        var text = name.ToString();

        // Assert
        text.ShouldBe("acme");
    }

    [Fact]
    public void Equals_ShouldBeTrue_WhenValuesAreEqual()
    {
        // Arrange
        var first = NamespaceName.Create("acme");
        var second = NamespaceName.Create("acme");

        // Act
        var equal = first == second;

        // Assert
        equal.ShouldBeTrue();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldBeFalse_WhenValuesDiffer()
    {
        // Arrange
        var first = NamespaceName.Create("acme");
        var second = NamespaceName.Create("plant-7");

        // Act
        var equal = first == second;

        // Assert
        equal.ShouldBeFalse();
    }
}
