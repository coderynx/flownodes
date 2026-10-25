using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Domain.Resources;

public sealed class ResourceNameTests
{
    [Theory]
    [InlineData("pump-1")]
    [InlineData("a")]
    [InlineData("0123456789012345678901234567890123456789012345678901234567890ab")]
    public void TryCreate_ShouldReturnName_WhenRawIsADnsLabel(string raw)
    {
        // Act
        var valid = ResourceName.TryCreate(raw, out var name);

        // Assert
        valid.ShouldBeTrue();
        name.ShouldNotBeNull().Value.ShouldBe(raw);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Pump")]
    [InlineData("pump_1")]
    [InlineData("-pump")]
    [InlineData("pump-")]
    [InlineData("0123456789012345678901234567890123456789012345678901234567890abc")]
    public void TryCreate_ShouldReturnFalse_WhenRawIsNotADnsLabel(string? raw)
    {
        // Act
        var valid = ResourceName.TryCreate(raw, out var name);

        // Assert
        valid.ShouldBeFalse();
        name.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenRawIsNotADnsLabel()
    {
        // Act
        var act = () => ResourceName.Create("Pump");

        // Assert
        act.ShouldThrow<ArgumentException>().ParamName.ShouldBe("raw");
    }

    [Fact]
    public void Equals_ShouldBeTrue_WhenValuesAreEqual()
    {
        // Act
        var equal = ResourceName.Create("pump-1") == ResourceName.Create("pump-1");

        // Assert
        equal.ShouldBeTrue();
    }
}
