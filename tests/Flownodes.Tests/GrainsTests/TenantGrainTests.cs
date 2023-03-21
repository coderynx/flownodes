using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Tenanting.Grains;
using Flownodes.Tests.Fixtures;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class TenantGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public TenantGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private FlownodesId NewFlownodesId => new(FlownodesEntity.Tenant, _fixture.Create<string>());
    private ITenantGrain NewTenantGrain => _cluster.GrainFactory.GetGrain<ITenantGrain>(NewFlownodesId);

    [Fact]
    public async Task UpdateMetadata_ShouldUpdateMetadata()
    {
        // Arrange.
        var grain = NewTenantGrain;
        var metadata = _fixture.Create<Dictionary<string, string?>>();

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
        var grain = NewTenantGrain;
        var metadata = _fixture.Create<Dictionary<string, string?>>();
        await grain.UpdateMetadataAsync(metadata);

        // Act.
        await grain.ClearMetadataAsync();

        // Assert.
        var newMetadata = await grain.GetMetadata();
        newMetadata.Should().NotBeEquivalentTo(metadata);
        newMetadata.Should().BeEmpty();
    }
}