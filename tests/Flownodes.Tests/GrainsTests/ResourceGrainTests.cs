using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing.Exceptions;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Tests.Fixtures;
using Flownodes.Tests.Interfaces;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class ResourceGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public ResourceGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private FlownodesId NewFlownodesId =>
        new(FlownodesEntity.Other, _fixture.Create<string>(), _fixture.Create<string>());

    private ITestResourceGrain NewTestResourceGrain =>
        _cluster.GrainFactory.GetGrain<ITestResourceGrain>(NewFlownodesId);

    [Fact]
    public async Task UpdateConfiguration_ShouldUpdateConfigurationAsync()
    {
        // Arrange.
        var grain = NewTestResourceGrain;
        var configuration = _fixture.Create<Dictionary<string, object?>>();

        // Act.
        await grain.UpdateConfigurationAsync(configuration);

        // Assert.
        var newConfiguration = await grain.GetConfiguration();
        newConfiguration.Should().BeEquivalentTo(configuration);
    }

    [Fact]
    public async Task UpdateConfiguration_ShouldUpdateConfiguration_WithBehaviourId()
    {
        // Arrange.
        var grain = NewTestResourceGrain;
        var configuration = _fixture.Create<Dictionary<string, object?>>();
        configuration.Add("behaviourId", "TestDeviceBehaviour");

        // Act & Assert.
        var act = async () => { await grain.UpdateConfigurationAsync(configuration); };
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task
        UpdateConfiguration_ShouldThrowResourceBehaviourNotRegisteredException_WhenBehaviourIsNotRegistered()
    {
        // Arrange.
        var id = new FlownodesId(FlownodesEntity.Device, "tenant", "device");
        var grain = _cluster.GrainFactory.GetGrain<IDeviceGrain>(id);

        // Act & Assert.
        var act = async () => { await grain.UpdateBehaviourId("unknown"); };
        await act.Should().ThrowAsync<ResourceBehaviourNotRegisteredException>();
    }

    [Fact]
    public async Task ClearConfiguration_ShouldClearConfiguration()
    {
        // Arrange.
        var grain = NewTestResourceGrain;
        var configuration = _fixture.Create<Dictionary<string, object?>>();
        await grain.UpdateConfigurationAsync(configuration);

        // Act.
        await grain.ClearConfigurationAsync();

        // Assert.
        var newConfiguration = await grain.GetConfiguration();
        newConfiguration.Should().NotBeEquivalentTo(configuration);
        newConfiguration.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateMetadata_ShouldUpdateMetadata()
    {
        // Arrange.
        var grain = NewTestResourceGrain;
        var metadata = _fixture.Create<Dictionary<string, object?>>();

        // Act.
        await grain.UpdateMetadataAsync(metadata);

        // Assert.
        var newMetadata = await grain.GetMetadata();
        newMetadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public async Task ClearMetadata_ShouldClearMetadata()
    {
        // Arrange.
        var grain = NewTestResourceGrain;
        var metadata = _fixture.Create<Dictionary<string, object?>>();
        await grain.UpdateMetadataAsync(metadata);

        // Act.
        await grain.ClearMetadataAsync();

        // Assert.
        var metadataTuple = await grain.GetMetadata();
        metadataTuple.Should().NotBeEquivalentTo(metadata);
        metadataTuple.Should().BeEmpty();
    }

    [Fact]
    public async Task GetState_ShouldGetState()
    {
        // Arrange.
        var grain = NewTestResourceGrain;

        // Act.
        var state = await grain.GetState();

        // Assert.
        state.Should().NotBeNull();
    }

    [Fact]
    public async Task ClearState_ShouldClearState()
    {
        // Arrange.
        var grain = NewTestResourceGrain;
        var state = _fixture.Create<Dictionary<string, object?>>();
        await grain.UpdateStateAsync(state);

        // Act.
        await grain.ClearStateAsync();

        // Assert.
        var newState = await grain.GetState();
        newState.Should().NotBeEquivalentTo(state);
        newState.Should().BeEmpty();
    }
}