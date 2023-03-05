using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Shared.Interfaces;
using Flownodes.Tests.Configuration;
using Flownodes.Tests.Interfaces;
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
        await manager.DeployResourceAsync<IDummyResourceGrain>(
            _fixture.Create<string>(),
            _fixture.Create<string>());
    }

    [Fact]
    public async Task DeployResource_ShouldDeployResourceWithValidId()
    {
        // Arrange.
        var manager = ProvideResourceManager();

        // Act.
        var resource = await manager.DeployResourceAsync<IDummyResourceGrain>(
            "tenant",
            "resource");

        var id = await resource.GetId();
        id.Should().Be("tenant/resource");
    }

    [Fact]
    public async Task DeployResource_ShouldThrowWhenResourceNameIsNullOrEmpty()
    {
        // Arrange.
        var manager = ProvideResourceManager();

        // Act & Assert.
        var act = async () => { await manager.DeployResourceAsync<IDummyResourceGrain>("tenant", string.Empty); };
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeployResource_ShouldNotThrowWhenConfigurationIsNull()
    {
        // Arrange.
        var manager = ProvideResourceManager();

        // Act & Assert.
        var act = async () =>
        {
            await manager.DeployResourceAsync<IDummyResourceGrain>("tenant", "resource",
                new Dictionary<string, object?>());
        };
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetResource_ShouldGetResourceGrain()
    {
        // Arrange.
        var manager = ProvideResourceManager();
        await manager.DeployResourceAsync<IDummyResourceGrain>("tenant", "resource");

        // Act.
        var grain = await manager.GetResourceAsync<IDummyResourceGrain>("tenant", "resource");

        // Assert.
        grain.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchResourcesByTags_ShouldReturnResources()
    {
        // Arrange.
        var manager = ProvideResourceManager();
        await manager.DeployResourceAsync<IDummyResourceGrain>("tenant", "resource_1");
        await manager.DeployResourceAsync<IDummyResourceGrain>("tenant", "resource_2");

        // Act.
        var result =
            await manager.SearchResourcesByTags("tenant",
                new HashSet<string> { "TestDeviceBehavior", "dummy_resource" });

        // Assert.
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetResource_ShouldReturnNullWhenResourceDoesNotExist()
    {
        // Arrange.
        var manager = ProvideResourceManager();
        await manager.DeployResourceAsync<IDummyResourceGrain>("tenant", "resource");

        // Act.
        var grain = await manager.GetResourceAsync<IDummyResourceGrain>("tenant", "resource1");

        // Assert.
        grain.Should().BeNull();
    }

    [Fact]
    public async Task RemoveResource_ShouldRemoveResource()
    {
        // Arrange.
        var manager = ProvideResourceManager();
        await manager.DeployResourceAsync<IDummyResourceGrain>("tenant", "resource");

        // Act.
        await manager.RemoveResourceAsync("tenant", "resource");

        var grain = await manager.GetResourceAsync<IDummyResourceGrain>("tenant", "resource");
        grain.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAllResources_ShouldRemoveAllResources()
    {
        // Arrange.
        var manager = ProvideResourceManager();
        await manager.DeployResourceAsync<IDummyResourceGrain>("tenant", "resource1");
        await manager.DeployResourceAsync<IDummyResourceGrain>("tenant", "resource2");

        // Act.
        await manager.RemoveAllResourcesAsync("tenant");

        // Assert.
        var grain1 = await manager.GetResourceAsync<IDummyResourceGrain>("tenant", "resource1");
        grain1.Should().BeNull();
        var grain2 = await manager.GetResourceAsync<IDummyResourceGrain>("tenant", "resource2");
        grain2.Should().BeNull();
    }
}