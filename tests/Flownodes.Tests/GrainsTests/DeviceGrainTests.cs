using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing.Exceptions;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Tests.Fixtures;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class DeviceGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public DeviceGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private EntityId NewEntityId =>
        new(Entity.Other, _fixture.Create<string>(), _fixture.Create<string>());

    private IDeviceGrain NewTestResourceGrain =>
        _cluster.GrainFactory.GetGrain<IDeviceGrain>(NewEntityId);

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
        var id = new EntityId(Entity.Device, "tenant", "device");
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
}