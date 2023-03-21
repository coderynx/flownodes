using System;
using Flownodes.Sdk.Entities;
using FluentAssertions;
using Xunit;

namespace Flownodes.Tests;

public class FlownodesIdTests
{
    [Fact]
    public void ConstructFlownodesIdFromEnumKind_ShouldConstruct()
    {
        // Arrange & Act.
        var id = new FlownodesId(FlownodesEntity.Other, "firstName", "secondName");

        // Act.
        id.IdString.Should().Be("firstName/other:secondName");
        id.EntityKind.Should().Be(FlownodesEntity.Other);
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
        id.EntityKind.Should().Be(FlownodesEntity.Other);
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

    [Theory]
    [InlineData(FlownodesEntity.TenantManager)]
    [InlineData(FlownodesEntity.ResourceManager)]
    [InlineData(FlownodesEntity.AlertManager)]
    [InlineData(FlownodesEntity.Tenant)]
    public void ConstructFlownodesId_ShouldConstruct_WhenIdIsShort(FlownodesEntity entityKind)
    {
        // Arrange & Act.
        var id = new FlownodesId(entityKind, "firstName");

        // Assert.
        id.EntityKind.Should().Be(entityKind);
        id.FirstName.Should().Be("firstName");
        id.SecondName.Should().BeNull();
    }
}