using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk;
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

    private FlownodesId ProvideFakeFlownodesId()
    {
        return new FlownodesId(FlownodesObject.Other, _fixture.Create<string>(), _fixture.Create<string>());
    }

    [Fact]
    public async Task GetConfiguration_ShouldReturnNullDateTime_WhenNotUpdated()
    {
        // Arrange.
        var grain = _cluster.GrainFactory.GetGrain<ITestResourceGrain>(ProvideFakeFlownodesId());

        // Act.
        var configurationTuple = await grain.GetConfiguration();

        // Assert.
        configurationTuple.Configuration.Should().BeEmpty();
        configurationTuple.LastUpdateDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateConfiguration_ShouldUpdateConfigurationAsync()
    {
        // Arrange.
        var grain = _cluster.GrainFactory.GetGrain<ITestResourceGrain>(ProvideFakeFlownodesId());
        var configuration = _fixture.Create<Dictionary<string, object?>>();

        // Act.
        await grain.UpdateConfigurationAsync(configuration);

        // Assert.
        var newConfiguration = await grain.GetConfiguration();
        newConfiguration.Configuration.Should().BeEquivalentTo(configuration);
    }

    [Fact]
    public async Task UpdateConfiguration_ShouldThrow_WhenBehaviourIsNotRegistered()
    {
        // Arrange.
        var grain = _cluster.GrainFactory.GetGrain<ITestResourceGrain>(ProvideFakeFlownodesId());
        var configuration = _fixture.Create<Dictionary<string, object?>>();
        configuration.Add("behaviourId", "unknown");

        // Act & Assert.
        var act = async () => { await grain.UpdateConfigurationAsync(configuration); };
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ClearConfiguration_ShouldClearConfiguration()
    {
        // Arrange.
        var grain = _cluster.GrainFactory.GetGrain<ITestResourceGrain>(ProvideFakeFlownodesId());
        var configuration = _fixture.Create<Dictionary<string, object?>>();
        await grain.UpdateConfigurationAsync(configuration);

        // Act.
        await grain.ClearConfigurationAsync();

        // Assert.
        var configurationTuple = await grain.GetConfiguration();
        configurationTuple.Configuration.Should().NotBeEquivalentTo(configuration);
        configurationTuple.Configuration.Should().BeEmpty();
        configurationTuple.LastUpdateDate.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMetadata_ShouldReturnNullDateTime_WhenNotUpdated()
    {
        // Arrange.
        var grain = _cluster.GrainFactory.GetGrain<ITestResourceGrain>(ProvideFakeFlownodesId());

        // Act.
        var configurationTuple = await grain.GetMetadata();

        // Assert.
        configurationTuple.Metadata.Should().BeEmpty();
        configurationTuple.LastUpdateDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateMetadata_ShouldUpdateMetadata()
    {
        // Arrange.
        var grain = _cluster.GrainFactory.GetGrain<ITestResourceGrain>(ProvideFakeFlownodesId());
        var metadata = _fixture.Create<Dictionary<string, string?>>();

        // Act.
        await grain.UpdateMetadataAsync(metadata);

        // Assert.
        var newMetadata = await grain.GetMetadata();
        newMetadata.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public async Task ClearMetadata_ShouldClearMetadata()
    {
        // Arrange.
        var grain = _cluster.GrainFactory.GetGrain<ITestResourceGrain>(ProvideFakeFlownodesId());
        var metadata = _fixture.Create<Dictionary<string, string?>>();
        await grain.UpdateMetadataAsync(metadata);

        // Act.
        await grain.ClearMetadataAsync();

        // Assert.
        var metadataTuple = await grain.GetMetadata();
        metadataTuple.Metadata.Should().NotBeEquivalentTo(metadata);
        metadataTuple.Metadata.Should().BeEmpty();
        metadataTuple.LastUpdateDate.Should().NotBeNull();
    }

    [Fact]
    public async Task GetState_ShouldGetState()
    {
        // Arrange.
        var grain = _cluster.GrainFactory.GetGrain<ITestResourceGrain>(ProvideFakeFlownodesId());

        // Act.
        var state = await grain.GetState();

        // Assert.
        state.Should().NotBeNull();
    }

    [Fact]
    public async Task GetState_ShouldReturnNullDateTime_WhenNotUpdated()
    {
        // Arrange.
        var grain = _cluster.GrainFactory.GetGrain<ITestResourceGrain>(ProvideFakeFlownodesId());

        // Act.
        var stateTuple = await grain.GetState();

        // Assert.
        stateTuple.State.Should().BeEmpty();
        stateTuple.LastUpdateDate.Should().BeNull();
    }

    [Fact]
    public async Task ClearState_ShouldClearState()
    {
        // Arrange.
        var grain = _cluster.GrainFactory.GetGrain<ITestResourceGrain>(ProvideFakeFlownodesId());
        var state = _fixture.Create<Dictionary<string, object?>>();
        await grain.UpdateStateAsync(state);

        // Act.
        await grain.ClearStateAsync();

        // Assert.
        var stateTuple = await grain.GetState();
        stateTuple.State.Should().NotBeEquivalentTo(state);
        stateTuple.State.Should().BeEmpty();
        stateTuple.LastUpdateDate.Should().NotBeNull();
    }
}