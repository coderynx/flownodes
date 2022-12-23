using System;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Flownodes.Tests.Configuration;
using Flownodes.Worker.Interfaces;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class ResourceManagerTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public ResourceManagerTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster;
        _fixture = new Fixture();
    }

    private IResourceManagerGrain ProvideResourceManager()
    {
        var grain = _cluster.GrainFactory.GetGrain<IResourceManagerGrain>(_fixture.Create<string>());
        return grain;
    }

    [Fact]
    public async Task DeployResource_ShouldDeployResource()
    {
        // Arrange.
        var manager = ProvideResourceManager();

        // Act & Assert.
        await manager.DeployResourceAsync<IDummyResourceGrain>(_fixture.Create<string>(), new ResourceConfigurationStore());
    }

    [Fact]
    public async Task DeployResource_ShouldThrowWhenIdIsNullOrWhitespace()
    {
        // Arrange.
        var manager = ProvideResourceManager();

        // Act & Assert.
        var act = async () =>
        {
            await manager.DeployResourceAsync<IDummyResourceGrain>(" ", new ResourceConfigurationStore());
        };
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeployResource_ShouldThrowWhenConfigurationIsNull()
    {
        // Arrange.
        var manager = ProvideResourceManager();

        // Act & Assert.
        var act = async () =>
        {
            await manager.DeployResourceAsync<IDummyResourceGrain>(_fixture.Create<string>(), null);
        };
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetResource_ShouldGetResourceGrain()
    {
        // Arrange.
        var manager = ProvideResourceManager();
        var id = _fixture.Create<string>();
        await manager.DeployResourceAsync<IDummyResourceGrain>(id, new ResourceConfigurationStore());

        // Act.
        var grain = await manager.GetResourceAsync<IDummyResourceGrain>(id);

        // Assert.
        grain.Should().NotBeNull();
    }

    [Fact]
    public async Task GetResource_ShouldReturnNullWhenResourceDoesNotExist()
    {
        // Arrange.
        var manager = ProvideResourceManager();
        await manager.DeployResourceAsync<IDummyResourceGrain>(_fixture.Create<string>(), new ResourceConfigurationStore());

        // Act.
        var grain = await manager.GetResourceAsync<IDummyResourceGrain>(_fixture.Create<string>());

        // Assert.
        grain.Should().BeNull();
    }

    [Fact]
    public async Task RemoveResource_ShouldRemoveResource()
    {
        // Arrange.
        var manager = ProvideResourceManager();
        var id = _fixture.Create<string>();
        await manager.DeployResourceAsync<IDummyResourceGrain>(id, new ResourceConfigurationStore());

        // Act.
        await manager.RemoveResourceAsync(id);

        var grain = await manager.GetResourceAsync<IDummyResourceGrain>(id);
        grain.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAllResources_ShouldRemoveAllResources()
    {
        // Arrange.
        var manager = ProvideResourceManager();
        var id1 = _fixture.Create<string>();
        await manager.DeployResourceAsync<IDummyResourceGrain>(id1, new ResourceConfigurationStore());
        var id2 = _fixture.Create<string>();
        await manager.DeployResourceAsync<IDummyResourceGrain>(id2, new ResourceConfigurationStore());

        // Act.
        await manager.RemoveAllResourcesAsync();

        // Assert.
        var grain1 = await manager.GetResourceAsync<IDummyResourceGrain>(id1);
        grain1.Should().BeNull();
        var grain2 = await manager.GetResourceAsync<IDummyResourceGrain>(id2);
        grain2.Should().BeNull();
    }
}