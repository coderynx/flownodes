using System;
using Flownodes.Sdk;
using FluentAssertions;
using Xunit;

namespace Flownodes.Tests;

public class FlownodesIdTests
{
    [Fact]
    public void ConstructFlownodesIdFromEnumKind_ShouldConstruct()
    {
        // Arrange & Act.
        var id = new FlownodesId(FlownodesObject.Other, "firstName", "secondName");

        // Act.
        id.IdString.Should().Be("firstName/other:secondName");
        id.ObjectKind.Should().Be(FlownodesObject.Other);
        id.FirstName.Should().Be("firstName");
        id.SecondName.Should().Be("secondName");
    }

    [Fact]
    public void ConstructFlownodesIdFromStringKind_ShouldConstruct()
    {
        // Arrange & Act.
        var id = new FlownodesId("other", "firstName", "secondName");

        // Assert.
        id.IdString.Should().Be("firstName/other:secondName");
        id.ObjectKind.Should().Be(FlownodesObject.Other);
        id.FirstName.Should().Be("firstName");
        id.SecondName.Should().Be("secondName");
    }

    [Fact]
    public void ConstructFlownodesIdFromStringKind_ShouldThrow_WhenInvalidKindIsPassed()
    {
        // Act.
        var act = () =>
        {
            var unused = new FlownodesId("invalid", "firstName", "secondName");
        };

        // Act & Assert.
        act.Should().Throw<ArgumentException>();
    }
}