using System.Collections.Generic;
using Flownodes.Worker.Extensions;
using FluentAssertions;
using Xunit;

namespace Flownodes.Tests;

public class ExtensionMethodTests
{
    [Fact]
    public void ContainsAll_ShouldReturnTrue_WhenSubDictIsContainedInMainDict()
    {
        // Arrange
        var mainDict = new Dictionary<string, object?>
        {
            { "a", 1 },
            { "b", "hello" },
            { "c", null },
            {
                "d", new Dictionary<string, object?>
                {
                    { "e", 2 },
                    { "f", "world" }
                }
            }
        };

        var subDict = new Dictionary<string, object?>
        {
            { "a", 1 },
            { "b", "hello" },
            { "c", null },
            {
                "d", new Dictionary<string, object?>
                {
                    { "e", 2 },
                    { "f", "world" }
                }
            }
        };

        // Act
        var result = mainDict.ContainsAll(subDict);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsAll_ShouldReturnFalse_WhenSubDictContainsExtraKey()
    {
        // Arrange
        var mainDict = new Dictionary<string, object?>
        {
            { "a", 1 },
            { "b", "hello" }
        };

        var subDict = new Dictionary<string, object?>
        {
            { "a", 1 },
            { "b", "hello" },
            { "c", "world" }
        };

        // Act
        var result = mainDict.ContainsAll(subDict);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsAll_ShouldReturnFalse_WhenSubDictHasKeyWithDifferentValue()
    {
        // Arrange
        var mainDict = new Dictionary<string, object?>
        {
            { "a", 1 },
            { "b", "hello" }
        };

        var subDict = new Dictionary<string, object?>
        {
            { "a", 1 },
            { "b", "world" }
        };

        // Act
        var result = mainDict.ContainsAll(subDict);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsAll_ShouldReturnFalse_WhenSubDictHasNestedDictWithDifferentValue()
    {
        // Arrange
        var mainDict = new Dictionary<string, object?>
        {
            {
                "a", new Dictionary<string, object?>
                {
                    { "b", "hello" }
                }
            }
        };

        var subDict = new Dictionary<string, object?>
        {
            {
                "a", new Dictionary<string, object?>
                {
                    { "b", "world" }
                }
            }
        };

        // Act
        var result = mainDict.ContainsAll(subDict);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsAll_ShouldReturnTrue_WhenSubDictHasNestedDictWithNullValue()
    {
        // Arrange
        var mainDict = new Dictionary<string, object?>
        {
            {
                "a", new Dictionary<string, object?>
                {
                    { "b", null }
                }
            }
        };

        var subDict = new Dictionary<string, object?>
        {
            {
                "a", new Dictionary<string, object?>
                {
                    { "b", null }
                }
            }
        };

        // Act
        var result = mainDict.ContainsAll(subDict);

        // Assert
        result.Should().BeTrue();
    }
}