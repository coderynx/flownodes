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
public class DeviceZoneGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public DeviceZoneGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private IResourceManagerGrain NewResourceManager =>
        _cluster.GrainFactory.GetGrain<IResourceManagerGrain>(new FlownodesId(FlownodesEntity.ResourceManager,
            _fixture.Create<string>()));

    [Fact]
    public async Task RegisterDevice_ShouldRegisterDevice()
    {
        // Arrange.
        var resourceManager = NewResourceManager;
        var device = await resourceManager.DeployResourceAsync<IDeviceGrain>("device");
        var zone = await resourceManager.DeployResourceAsync<IDeviceZoneGrain>("device_zone");

        // Act.
        await zone.RegisterDeviceAsync(await device.GetId());

        // Assert.
        var registered = await zone.GetDeviceAsync(await device.GetId());
        registered.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterDevice_ShouldThrowException_WhenDeviceDoesNotExists()
    {
        // Arrange.
        var resourceManager = NewResourceManager;
        var zone = await resourceManager.DeployResourceAsync<IDeviceZoneGrain>("device_zone");

        // Act & Assert.
        var id = new FlownodesId(FlownodesEntity.Device, "tenant", "device");
        var act = async () => await zone.RegisterDeviceAsync(id);
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task UnregisterDevice_ShouldUnregisterDevice()
    {
        // Arrange.
        var resourceManager = NewResourceManager;
        var device = await resourceManager.DeployResourceAsync<IDeviceGrain>("device");
        var zone = await resourceManager.DeployResourceAsync<IDeviceZoneGrain>("device_zone");
        await zone.RegisterDeviceAsync(await device.GetId());

        // Act.
        await zone.UnregisterDeviceAsync(await device.GetId());

        // Assert.
        var registered = await zone.GetDeviceAsync(await device.GetId());
        registered.Should().BeNull();
    }
}