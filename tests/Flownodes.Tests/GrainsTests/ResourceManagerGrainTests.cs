using System;
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
public class ResourceManagerGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public ResourceManagerGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private FlownodesId NewFlownodesId => new(FlownodesEntity.ResourceManager, _fixture.Create<string>());

    private IResourceManagerGrain NewResourceManagerGrain =>
        _cluster.GrainFactory.GetGrain<IResourceManagerGrain>(NewFlownodesId);

    [Fact]
    public async Task DeployResource_ShouldDeployAssetGrain_WithCorrectId()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;

        // Act.
        var grain = await manager.DeployResourceAsync<IAssetGrain>(_fixture.Create<string>());
        
        // Assert.
        grain.Should().NotBeNull();
        var id = await grain.GetId();
        id.IsManager.Should().Be(false);
        id.EntityKind.Should().Be(FlownodesEntity.Asset);
    }

    [Fact]
    public async Task DeployResource_ShouldDeployDataSourceGrain_WithCorrectId()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;

        // Act.
        var grain = await manager.DeployResourceAsync<IDataSourceGrain>(_fixture.Create<string>());
        
        // Assert.
        grain.Should().NotBeNull();
        var id = await grain.GetId();
        id.IsManager.Should().Be(false);
        id.EntityKind.Should().Be(FlownodesEntity.DataSource);
    }
    
    [Fact]
    public async Task DeployResource_ShouldDeployResourceGroupGrain_WithCorrectId()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;

        // Act.
        var grain = await manager.DeployResourceAsync<IResourceGroupGrain>(_fixture.Create<string>());

        // Assert.
        grain.Should().NotBeNull();
        var id = await grain.GetId();
        id.IsManager.Should().Be(false);
        id.EntityKind.Should().Be(FlownodesEntity.ResourceGroup);
    }
    
    [Fact]
    public async Task DeployResource_ShouldDeployDeviceGrain_WithCorrectId()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;

        // Act.
        var grain = await manager.DeployResourceAsync<IDeviceGrain>(_fixture.Create<string>());

        // Assert.
        grain.Should().NotBeNull();
        var id = await grain.GetId();
        id.IsManager.Should().Be(false);
        id.EntityKind.Should().Be(FlownodesEntity.Device);
    }

    [Fact]
    public async Task DeployResource_ShouldThrowArgumentException_WhenResourceNameIsNullOrEmpty()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;

        // Act & Assert.
        var act = async () => { await manager.DeployResourceAsync<ITestResourceGrain>(string.Empty); };
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeployResource_ShouldNotThrow_WhenConfigurationIsNull()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;

        // Act & Assert.
        var act = async () =>
        {
            await manager.DeployResourceAsync<ITestResourceGrain>("resource", new Dictionary<string, object?>());
        };
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeployResource_ShouldThrowResourceAlreadyRegisteredException_WhenResourceIsAlreadyRegistered()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;
        await manager.DeployResourceAsync<ITestResourceGrain>("resource");
        
        // Act & Assert.
        var act = async () => { await manager.DeployResourceAsync<ITestResourceGrain>("resource"); };
        await act.Should().ThrowAsync<ResourceAlreadyRegisteredException>();
    }

    [Fact]
    public async Task GetResource_ShouldGetResourceGrain()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;
        await manager.DeployResourceAsync<ITestResourceGrain>("resource");

        // Act.
        var grain = await manager.GetResourceAsync<ITestResourceGrain>("resource");

        // Assert.
        grain.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchResourcesByTags_ShouldReturnResources()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;
        await manager.DeployResourceAsync<ITestResourceGrain>("resource_1");
        await manager.DeployResourceAsync<ITestResourceGrain>("resource_2");

        // Act.
        var result =
            await manager.SearchResourcesByTags(new HashSet<string> { "other" });

        // Assert.
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetResource_ShouldReturnNull_WhenResourceDoesNotExist()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;
        await manager.DeployResourceAsync<ITestResourceGrain>("resource");

        // Act.
        var grain = await manager.GetResourceAsync<ITestResourceGrain>("resource1");

        // Assert.
        grain.Should().BeNull();
    }

    [Fact]
    public async Task RemoveResource_ShouldRemoveResource()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;
        await manager.DeployResourceAsync<ITestResourceGrain>("resource");

        // Act.
        await manager.RemoveResourceAsync("resource");

        var grain = await manager.GetResourceAsync<ITestResourceGrain>("resource");
        grain.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAllResources_ShouldRemoveAllResources()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;
        await manager.DeployResourceAsync<ITestResourceGrain>("resource1");
        await manager.DeployResourceAsync<ITestResourceGrain>("resource2");

        // Act.
        await manager.RemoveAllResourcesAsync();

        // Assert.
        var grain1 = await manager.GetResourceAsync<ITestResourceGrain>("resource1");
        grain1.Should().BeNull();
        var grain2 = await manager.GetResourceAsync<ITestResourceGrain>("resource2");
        grain2.Should().BeNull();
    }

    [Fact]
    public async Task GetAllResourceSummaries_ShouldGetAllResourceSummaries()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;
        await manager.DeployResourceAsync<ITestResourceGrain>("resource1");
        await manager.DeployResourceAsync<ITestResourceGrain>("resource2");

        // Act.
        var summaries = await manager.GetAllResourceSummaries();

        // Assert.
        summaries.Should().HaveCount(2);
    }
}