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
        var id = new FlownodesId(FlownodesObject.Other, "firstName", "secondName");
        id.Should().NotBeNull();
    }

    [Fact]
    public void ConstructFlownodesIdFromStringKind_ShouldConstruct()
    {
        var id = new FlownodesId("other", "firstName", "secondName");
        id.Should().NotBeNull();
    }

    [Fact]
    public void ConstructFlownodesIdFromStringKind_ShouldThrowWhenInvalidKindIsPassed()
    {
        var act = () =>
        {
            var unused = new FlownodesId("invalid", "firstName", "secondName");
        };

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Id_ShouldGetCorrectIdString()
    {
        var id = new FlownodesId("other", "firstName", "secondName");

        id.Id.Should().Be("firstName/other:secondName");
    }

    [Fact]
    public void FirstName_ShouldGetCorrectFirstNameString()
    {
        var id = new FlownodesId("other", "firstName", "secondName");

        id.FirstName.Should().Be("firstName");
    }

    [Fact]
    public void SecondName_ShouldGetCorrectSecondNameString()
    {
        var id = new FlownodesId("other", "firstName", "secondName");

        id.SecondName.Should().Be("secondName");
    }
}