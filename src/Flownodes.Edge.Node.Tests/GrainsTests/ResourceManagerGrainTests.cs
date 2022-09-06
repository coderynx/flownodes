using System;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Edge.Core.Resources;
using Flownodes.Edge.Node.Tests.Configuration;
using FluentAssertions;
using Orleans;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Edge.Node.Tests.GrainsTests;

[Collection("TestCluster")]
public class ResourceManagerGrainTests : IClassFixture<ClusterFixture>
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public ResourceManagerGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster;
        _fixture = new Fixture();
    }

    private IResourceManagerGrain ProvideResourceManager()
    {
        return _cluster.GrainFactory.GetGrain<IResourceManagerGrain>(_fixture.Create<string>());
    }

    [Fact]
    public void ShouldActivate()
    {
        // Arrange & act.
        var resourceManager = _cluster.GrainFactory.GetGrain<IResourceManagerGrain>(_fixture.Create<string>());

        // Assert.
        resourceManager.GetGrainIdentity().Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldRegisterDevice()
    {
        // Arrange.
        var resourceManager = ProvideResourceManager();

        // Act.
        var deviceGrain = await resourceManager.RegisterDeviceAsync(_fixture.Create<string>(), "TestDeviceBehavior");

        // Assert.
        deviceGrain.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldThrowOnRegisterDevice_WhenDeviceExists()
    {
        // Arrange.
        var resourceManager = ProvideResourceManager();
        await resourceManager.RegisterDeviceAsync(Globals.TestingFrn, "TestDeviceBehavior");

        // Act & Assert.
        await resourceManager.Invoking(g => g.RegisterDeviceAsync(Globals.TestingFrn, "TestDeviceBehavior"))
            .Should()
            .ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ShouldThrowOnRegisterDevice_WhenBehaviorIdDoesNotExists()
    {
        // Arrange.
        var resourceManager = ProvideResourceManager();

        // Act & Assert.
        await resourceManager.Invoking(g => g.RegisterDeviceAsync(Globals.TestingFrn, "TestDeviceBehavior1")).Should()
            .ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ShouldThrowOnRegisterDevice_WhenIdOrBehaviorIdAreNullOrWhitespace()
    {
        // Arrange.
        var resourceManager = ProvideResourceManager();

        // Act & Assert.
        await resourceManager.Invoking(g => g.RegisterDeviceAsync(string.Empty, string.Empty)).Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ShouldRegisterAsset()
    {
        // Arrange.
        var resourceManager = ProvideResourceManager();

        // Act.
        var asset =
            await resourceManager.RegisterAssetAsync(_fixture.Create<string>());

        // Assert.
        asset.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldThrowOnRegisterAsset_WhenAssetExists()
    {
        // Arrange.
        var resourceManager = ProvideResourceManager();
        await resourceManager.RegisterAssetAsync(Globals.TestingFrn);

        // Act & Assert.
        await resourceManager.Invoking(g => g.RegisterAssetAsync(Globals.TestingFrn))
            .Should()
            .ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ShouldThrowOnRegisterAsset_WhenIdsNullOrWhitespace()
    {
        // Arrange.
        var resourceManager = ProvideResourceManager();

        // Act & Assert.
        await resourceManager.Invoking(g => g.RegisterAssetAsync(string.Empty)).Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ShouldRemoveDevice()
    {
        // Arrange.
        var resourceManager = ProvideResourceManager();

        var id = _fixture.Create<string>();
        await resourceManager.RegisterDeviceAsync(id, "TestDeviceBehavior");

        // Act.
        await resourceManager.RemoveDeviceAsync(id);

        // Assert.
        var device = await resourceManager.GetDevice(id);
        device.Should().BeNull();
    }

    [Fact]
    public async Task ShouldThrowOnRemoveDevice_WhenIdDoesNotExists()
    {
        // Arrange.
        var resourceManager = ProvideResourceManager();

        var id = _fixture.Create<string>();
        await resourceManager.RegisterDeviceAsync(id, "TestDeviceBehavior");

        // Act & Assert.
        await resourceManager.Invoking(g => g.RemoveDeviceAsync(_fixture.Create<string>())).Should()
            .ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ShouldThrowOnRemoveDevice_WhenIdIsNullOrWhitespace()
    {
        // Arrange.
        var resourceManager = ProvideResourceManager();

        var id = _fixture.Create<string>();
        await resourceManager.RegisterDeviceAsync(id, "TestDeviceBehavior");

        // Act & Assert.
        await resourceManager.Invoking(g => g.RemoveDeviceAsync(string.Empty)).Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ShouldRemoveAsset()
    {
        // Arrange.
        var resourceManager = ProvideResourceManager();

        var id = _fixture.Create<string>();
        await resourceManager.RegisterAssetAsync(id);

        // Act.
        await resourceManager.RemoveAssetAsync(id);

        // Assert.
        var device = await resourceManager.GetAsset(id);
        device.Should().BeNull();
    }

    [Fact]
    public async Task ShouldThrowOnRemoveAsset_WhenIdDoesNotExists()
    {
        // Arrange.
        var resourceManager = ProvideResourceManager();

        var id = _fixture.Create<string>();
        await resourceManager.RegisterAssetAsync(id);

        // Act & Assert.
        await resourceManager.Invoking(g => g.RemoveAssetAsync(_fixture.Create<string>())).Should()
            .ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ShouldThrowOnRemoveAsset_WhenIdIsNullOrWhitespace()
    {
        // Arrange.
        var resourceManager = ProvideResourceManager();

        var id = _fixture.Create<string>();
        await resourceManager.RegisterAssetAsync(id);

        // Act & Assert.
        await resourceManager.Invoking(g => g.RemoveAssetAsync(string.Empty)).Should()
            .ThrowAsync<ArgumentException>();
    }
}