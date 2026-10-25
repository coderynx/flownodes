using Engine.Domain.Integrations;
using Xunit;

namespace Engine.UnitTests.Domain;

public sealed class SdkCompatibilityTests
{
    [Theory]
    [InlineData("0.1.0", "0.1.0")]
    [InlineData("0.1.4", "0.1.0")]
    [InlineData("1.2.0", "1.0.0")]
    [InlineData("1.2.0", "1.5.0")]
    public void Supports_ShouldAccept_WhenTheVersionsAreCompatible(string host, string builtAgainst)
    {
        // Act
        var supported = SdkCompatibility.Supports(Version.Parse(host), Version.Parse(builtAgainst));

        // Assert
        supported.ShouldBeTrue();
    }

    [Theory]
    [InlineData("0.1.0", "0.2.0")]
    [InlineData("0.2.0", "0.1.0")]
    [InlineData("1.0.0", "2.0.0")]
    [InlineData("1.0.0", "0.1.0")]
    public void Supports_ShouldRefuse_WhenTheVersionsAreIncompatible(string host, string builtAgainst)
    {
        // Act
        var supported = SdkCompatibility.Supports(Version.Parse(host), Version.Parse(builtAgainst));

        // Assert
        supported.ShouldBeFalse();
    }
}
