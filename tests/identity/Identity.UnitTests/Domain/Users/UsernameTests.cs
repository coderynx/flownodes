using Identity.Domain.Users;
using Xunit;

namespace Identity.UnitTests.Domain.Users;

public sealed class UsernameTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("alice")]
    [InlineData("Alice.Smith_2-b@example")]
    [InlineData("0")]
    public void TryCreate_ShouldAcceptTheText_WhenItUsesOnlyAllowedCharacters(string raw)
    {
        // Act
        var valid = Username.TryCreate(raw, out var username);

        // Assert
        valid.ShouldBeTrue();
        username.ShouldNotBeNull().Value.ShouldBe(raw);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("alice smith")]
    [InlineData("alice/smith")]
    [InlineData("alicé")]
    [InlineData("alice\tsmith")]
    [InlineData("alice\n")]
    public void TryCreate_ShouldRejectTheText_WhenItIsNotAUsername(string? raw)
    {
        // Act
        var valid = Username.TryCreate(raw, out var username);

        // Assert
        valid.ShouldBeFalse();
        username.ShouldBeNull();
    }

    [Fact]
    public void TryCreate_ShouldAcceptTheText_WhenItHasSixtyFourCharacters()
    {
        // Arrange
        var raw = new string('a', 64);

        // Act
        var valid = Username.TryCreate(raw, out _);

        // Assert
        valid.ShouldBeTrue();
    }

    [Fact]
    public void TryCreate_ShouldRejectTheText_WhenItHasSixtyFiveCharacters()
    {
        // Arrange
        var raw = new string('a', 65);

        // Act
        var valid = Username.TryCreate(raw, out _);

        // Assert
        valid.ShouldBeFalse();
    }

    [Fact]
    public void Normalized_ShouldBeTheUpperCaseValue()
    {
        // Arrange
        var username = Username.Create("Alice.b");

        // Act
        var normalized = username.Normalized;

        // Assert
        normalized.ShouldBe("ALICE.B");
        username.Value.ShouldBe("Alice.b");
    }

    [Fact]
    public void Normalized_ShouldBeTheSame_WhenTwoUsernamesDifferOnlyInCase()
    {
        // Arrange
        var lower = Username.Create("alice");
        var mixed = Username.Create("ALice");

        // Act
        var same = lower.Normalized == mixed.Normalized;

        // Assert
        same.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldReturnTheUsername_WhenTheTextIsValid()
    {
        // Act
        var username = Username.Create("alice");

        // Assert
        username.Value.ShouldBe("alice");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenTheTextIsNotAUsername()
    {
        // Act
        var act = () => Username.Create("alice smith");

        // Assert
        act.ShouldThrow<ArgumentException>().ParamName.ShouldBe("raw");
    }

    [Fact]
    public void ToString_ShouldReturnTheValue()
    {
        // Arrange
        var username = Username.Create("Alice");

        // Act
        var text = username.ToString();

        // Assert
        text.ShouldBe("Alice");
    }

    [Fact]
    public void Equals_ShouldBeTrue_WhenTwoUsernamesHaveTheSameValue()
    {
        // Arrange
        var first = Username.Create("alice");
        var second = Username.Create("alice");

        // Act
        var equal = first.Equals(second);

        // Assert
        equal.ShouldBeTrue();
    }
}
