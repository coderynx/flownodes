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
public class ResourceGroupGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public ResourceGroupGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private IResourceManagerGrain NewResourceManager =>
        _cluster.GrainFactory.GetGrain<IResourceManagerGrain>(new FlownodesId(FlownodesEntity.ResourceManager,
            _fixture.Create<string>()));

    [Fact]
    public async Task RegisterResource_ShouldRegisterResource()
    {
        // Arrange.
        var resourceManager = NewResourceManager;
        var resourceGroup = await resourceManager.DeployResourceAsync<IResourceGroupGrain>("resource_group");
        await resourceManager.DeployResourceAsync<IAssetGrain>("test_resource");

        // Act.
        await resourceGroup.RegisterResourceAsync("test_resource");

        // Assert.
        var registrations = await resourceGroup.GetRegistrations();
        registrations.Should().ContainSingle();
    }

    [Fact]
    public async Task RegisterResource_ShouldThrowResourceNotFoundException_WhenResourceIsNotDeployed()
    {
        // Arrange.
        var resourceManager = NewResourceManager;
        var resourceGroup = await resourceManager.DeployResourceAsync<IResourceGroupGrain>("resource_group");

        // Act & Assert.
        var act = async () => { await resourceGroup.RegisterResourceAsync("test_resource"); };

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task UnregisterResource_ShouldUnregisterResource()
    {
        // Arrange.
        var resourceManager = NewResourceManager;
        var resourceGroup = await resourceManager.DeployResourceAsync<IResourceGroupGrain>("resource_group");
        await resourceManager.DeployResourceAsync<IAssetGrain>("test_resource");
        await resourceGroup.RegisterResourceAsync("test_resource");

        // Act.
        await resourceGroup.UnregisterResourceAsync("test_resource");

        // Assert.
        var registrations = await resourceGroup.GetRegistrations();
        registrations.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearRegistrations_ShouldClearRegistrations()
    {
        // Arrange.
        var resourceManager = NewResourceManager;
        var resourceGroup = await resourceManager.DeployResourceAsync<IResourceGroupGrain>("resource_group");
        await resourceManager.DeployResourceAsync<IAssetGrain>("test_resource_1");
        await resourceManager.DeployResourceAsync<IAssetGrain>("test_resource_2");
        await resourceGroup.RegisterResourceAsync("test_resource_1");
        await resourceGroup.RegisterResourceAsync("test_resource_2");

        // Act.
        await resourceGroup.ClearRegistrationsAsync();

        // Assert.
        var registrations = await resourceGroup.GetRegistrations();
        registrations.Should().BeEmpty();
    }

    [Fact]
    public async Task GetResource_ShouldGetResource()
    {
        // Arrange.
        var resourceManager = NewResourceManager;
        var resourceGroup = await resourceManager.DeployResourceAsync<IResourceGroupGrain>("resource_group");
        await resourceManager.DeployResourceAsync<IAssetGrain>("test_resource");
        await resourceGroup.RegisterResourceAsync("test_resource");

        // Act.
        var resource = await resourceGroup.GetResourceAsync("test_resource");

        // Assert.
        resource.Should().NotBeNull();
    }
}