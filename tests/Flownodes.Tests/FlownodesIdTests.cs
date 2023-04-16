using System;
using Flownodes.Sdk.Entities;
using FluentAssertions;
using Xunit;

namespace Flownodes.Tests;

public class FlownodesIdTests
{
    [Theory]
    [InlineData(Entity.Device)]
    [InlineData(Entity.ResourceGroup)]
    [InlineData(Entity.DataSource)]
    [InlineData(Entity.Asset)]
    public void ConstructFlownodesId_ShouldConstruct_WithFullId(Entity entityKind)
    {
        // Arrange & Act.
        var id = new EntityId(entityKind, "firstName", "secondName");

        // Act.
        id.IdString.Should().Be($"firstName/{EntityId.KindToString(entityKind)}:secondName");
        id.EntityKind.Should().Be(entityKind);
        id.FirstName.Should().Be("firstName");
        id.SecondName.Should().Be("secondName");
    }

    [Theory]
    [InlineData(EntityNames.Device)]
    [InlineData(EntityNames.ResourceGroup)]
    [InlineData(EntityNames.DataSource)]
    [InlineData(EntityNames.Asset)]
    public void ConstructFlownodesIdFromStringKind_ShouldConstruct(string entityKind)
    {
        // Arrange & Act.
        var id = new EntityId(entityKind, "firstName", "secondName");

        // Assert.
        id.IdString.Should().Be($"firstName/{entityKind}:secondName");
        id.EntityKind.Should().Be(EntityId.KindFromString(entityKind));
        id.FirstName.Should().Be("firstName");
        id.SecondName.Should().Be("secondName");
    }

    [Theory]
    [InlineData(Entity.TenantManager)]
    [InlineData(Entity.ResourceManager)]
    [InlineData(Entity.AlertManager)]
    [InlineData(Entity.Tenant)]
    public void ConstructFlownodesId_ShouldConstruct_WhitShortId(Entity entityKind)
    {
        // Arrange & Act.
        var id = new EntityId(entityKind, "firstName");

        // Assert.
        id.EntityKind.Should().Be(entityKind);
        id.FirstName.Should().Be("firstName");
        id.SecondName.Should().BeNull();
    }

    [Fact]
    public void ConstructFlownodesIdFromStringKind_ShouldThrow_WhenInvalidKindIsPassed()
    {
        // Act.
        var act = () =>
        {
            var unused = new EntityId("invalid", "firstName", "secondName");
        };

        // Act & Assert.
        act.Should().Throw<ArgumentException>();
    }
}