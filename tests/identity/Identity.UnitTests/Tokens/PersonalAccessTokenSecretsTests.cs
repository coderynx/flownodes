using Identity.Application.Tokens;
using Xunit;

namespace Identity.UnitTests.Tokens;

public sealed class PersonalAccessTokenSecretsTests
{
    private readonly PersonalAccessTokenSecrets _secrets = new();

    [Fact]
    public void Generate_ShouldReturnADifferentPrefixedSecret_WhenCalledTwice()
    {
        // Act
        var first = _secrets.Generate();
        var second = _secrets.Generate();

        // Assert
        first.ShouldStartWith("fnpat_");
        second.ShouldNotBe(first);
    }

    [Fact]
    public void Hash_ShouldReturnTheSameHashForTheSameSecret_WhenCalledTwice()
    {
        // Act
        var first = _secrets.Hash(TokenDoubles.Secret);
        var second = _secrets.Hash(TokenDoubles.Secret);

        // Assert
        first.ShouldBe(second);
        first.ShouldNotBe(_secrets.Hash("fnpat_another-secret"));
    }
}
