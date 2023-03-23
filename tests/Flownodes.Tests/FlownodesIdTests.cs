using System;
using Flownodes.Sdk.Entities;
using Flownodes.Worker.Builders;
using FluentAssertions;
using Xunit;

namespace Flownodes.Tests;

public class FlownodesIdTests
{

    [Theory]
    [InlineData(FlownodesEntity.Device)]
    [InlineData(FlownodesEntity.ResourceGroup)]
    [InlineData(FlownodesEntity.DataSource)]
    [InlineData(FlownodesEntity.Asset)]
    public void ConstructFlownodesId_ShouldConstruct_WithFullId(FlownodesEntity entityKind)
    {
        // Arrange & Act.
        var id = new FlownodesId(entityKind, "firstName", "secondName");

        // Act.
        id.IdString.Should().Be($"firstName/{FlownodesId.KindToString(entityKind)}:secondName");
        id.EntityKind.Should().Be(entityKind);
        id.FirstName.Should().Be("firstName");
        id.SecondName.Should().Be("secondName");
    }

    [Theory]
    [InlineData(FlownodesEntityNames.Device)]
    [InlineData(FlownodesEntityNames.ResourceGroup)]
    [InlineData(FlownodesEntityNames.DataSource)]
    [InlineData(FlownodesEntityNames.Asset)]
    public void ConstructFlownodesIdFromStringKind_ShouldConstruct(string entityKind)
    {
        // Arrange & Act.
        var id = new FlownodesId(entityKind, "firstName", "secondName");

        // Assert.
        id.IdString.Should().Be($"firstName/{entityKind}:secondName");
        id.EntityKind.Should().Be(FlownodesId.KindFromString(entityKind));
        id.FirstName.Should().Be("firstName");
        id.SecondName.Should().Be("secondName");
    }

    [Theory]
    [InlineData(FlownodesEntity.TenantManager)]
    [InlineData(FlownodesEntity.ResourceManager)]
    [InlineData(FlownodesEntity.AlertManager)]
    [InlineData(FlownodesEntity.Tenant)]
    public void ConstructFlownodesId_ShouldConstruct_WhitShortId(FlownodesEntity entityKind)
    {
        // Arrange & Act.
        var id = new FlownodesId(entityKind, "firstName");

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
            var unused = new FlownodesId("invalid", "firstName", "secondName");
        };

        // Act & Assert.
        act.Should().Throw<ArgumentException>();
    }
}