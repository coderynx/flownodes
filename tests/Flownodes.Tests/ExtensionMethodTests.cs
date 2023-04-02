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
        // Arrange.
        var main = new Dictionary<string, object?>
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

        var secondary = new Dictionary<string, object?>
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

        // Act.
        var result = main.ContainsAll(secondary);

        // Assert.
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsAll_ShouldReturnFalse_WhenSubDictContainsExtraKey()
    {
        // Arrange.
        var main = new Dictionary<string, object?>
        {
            { "a", 1 },
            { "b", "hello" }
        };

        var secondary = new Dictionary<string, object?>
        {
            { "a", 1 },
            { "b", "hello" },
            { "c", "world" }
        };

        // Act.
        var result = main.ContainsAll(secondary);

        // Assert.
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsAll_ShouldReturnFalse_WhenSubDictHasKeyWithDifferentValue()
    {
        // Arrange.
        var main = new Dictionary<string, object?>
        {
            { "a", 1 },
            { "b", "hello" }
        };

        var secondary = new Dictionary<string, object?>
        {
            { "a", 1 },
            { "b", "world" }
        };

        // Act.
        var result = main.ContainsAll(secondary);

        // Assert.
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsAll_ShouldReturnFalse_WhenSubDictHasNestedDictWithDifferentValue()
    {
        // Arrange.
        var main = new Dictionary<string, object?>
        {
            {
                "a", new Dictionary<string, object?>
                {
                    { "b", "hello" }
                }
            }
        };

        var secondary = new Dictionary<string, object?>
        {
            {
                "a", new Dictionary<string, object?>
                {
                    { "b", "world" }
                }
            }
        };

        // Act.
        var result = main.ContainsAll(secondary);

        // Assert.
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsAll_ShouldReturnTrue_WhenSubDictHasNestedDictWithNullValue()
    {
        // Arrange.
        var main = new Dictionary<string, object?>
        {
            {
                "a", new Dictionary<string, object?>
                {
                    { "b", null }
                }
            }
        };

        var secondary = new Dictionary<string, object?>
        {
            {
                "a", new Dictionary<string, object?>
                {
                    { "b", null }
                }
            }
        };

        // Act.
        var result = main.ContainsAll(secondary);

        // Assert.
        result.Should().BeTrue();
    }
}