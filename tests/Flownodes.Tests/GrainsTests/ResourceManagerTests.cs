using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk;
using Flownodes.Shared.Resourcing;
using Flownodes.Tests.Fixtures;
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
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private FlownodesId NewFlownodesId => new(FlownodesObject.ResourceManager, _fixture.Create<string>());

    private IResourceManagerGrain NewResourceManagerGrain =>
        _cluster.GrainFactory.GetGrain<IResourceManagerGrain>(NewFlownodesId);

    [Fact]
    public async Task DeployResource_ShouldDeployResource()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;

        // Act & Assert.
        await manager.DeployResourceAsync<ITestResourceGrain>(
            _fixture.Create<string>(),
            _fixture.Create<string>());
    }

    [Fact]
    public async Task DeployResource_ShouldDeployResourceWithValidId()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;

        // Act.
        var resource = await manager.DeployResourceAsync<ITestResourceGrain>(
            "tenant",
            "resource");

        // Assert.
        var id = await resource.GetId();
        id.IdString.Should().Be("tenant/other:resource");
    }

    [Fact]
    public async Task DeployResource_ShouldThrow_WhenResourceNameIsNullOrEmpty()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;

        // Act & Assert.
        var act = async () => { await manager.DeployResourceAsync<ITestResourceGrain>("tenant", string.Empty); };
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
            await manager.DeployResourceAsync<ITestResourceGrain>("tenant", "resource",
                new Dictionary<string, object?>());
        };
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetResource_ShouldGetResourceGrain()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;
        await manager.DeployResourceAsync<ITestResourceGrain>("tenant", "resource");

        // Act.
        var grain = await manager.GetResourceAsync<ITestResourceGrain>("tenant", "resource");

        // Assert.
        grain.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchResourcesByTags_ShouldReturnResources()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;
        await manager.DeployResourceAsync<ITestResourceGrain>("tenant", "resource_1");
        await manager.DeployResourceAsync<ITestResourceGrain>("tenant", "resource_2");

        // Act.
        var result =
            await manager.SearchResourcesByTags("tenant",
                new HashSet<string> { "TestDeviceBehavior", "other" });

        // Assert.
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetResource_ShouldReturnNull_WhenResourceDoesNotExist()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;
        await manager.DeployResourceAsync<ITestResourceGrain>("tenant", "resource");

        // Act.
        var grain = await manager.GetResourceAsync<ITestResourceGrain>("tenant", "resource1");

        // Assert.
        grain.Should().BeNull();
    }

    [Fact]
    public async Task RemoveResource_ShouldRemoveResource()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;
        await manager.DeployResourceAsync<ITestResourceGrain>("tenant", "resource");

        // Act.
        await manager.RemoveResourceAsync("tenant", "resource");

        var grain = await manager.GetResourceAsync<ITestResourceGrain>("tenant", "resource");
        grain.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAllResources_ShouldRemoveAllResources()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;
        await manager.DeployResourceAsync<ITestResourceGrain>("tenant", "resource1");
        await manager.DeployResourceAsync<ITestResourceGrain>("tenant", "resource2");

        // Act.
        await manager.RemoveAllResourcesAsync("tenant");

        // Assert.
        var grain1 = await manager.GetResourceAsync<ITestResourceGrain>("tenant", "resource1");
        grain1.Should().BeNull();
        var grain2 = await manager.GetResourceAsync<ITestResourceGrain>("tenant", "resource2");
        grain2.Should().BeNull();
    }

    [Fact]
    public async Task GetAllResourceSummaries_ShouldGetAllResourceSummaries()
    {
        // Arrange.
        var manager = NewResourceManagerGrain;
        await manager.DeployResourceAsync<ITestResourceGrain>("tenant", "resource1");
        await manager.DeployResourceAsync<ITestResourceGrain>("tenant", "resource2");

        // Act.
        var summaries = await manager.GetAllResourceSummaries("tenant");

        // Assert.
        summaries.Should().HaveCount(2);
    }
}