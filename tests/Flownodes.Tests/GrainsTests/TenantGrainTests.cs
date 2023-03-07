using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Shared.Interfaces;
using Flownodes.Tests.Configuration;
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

    [Fact]
    public async Task UpdateMetadata_ShouldUpdateMetadata()
    {
        var grain = _cluster.GrainFactory.GetGrain<ITenantGrain>(_fixture.Create<string>());

        var metadata = _fixture.Create<Dictionary<string, string?>>();
        await grain.UpdateMetadataAsync(metadata);

        var newMetadata = await grain.GetMetadata();
        newMetadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public async Task ClearMetadata_ShouldClearMetadata()
    {
        var grain = _cluster.GrainFactory.GetGrain<ITenantGrain>(_fixture.Create<string>());

        var metadata = _fixture.Create<Dictionary<string, string?>>();
        await grain.UpdateMetadataAsync(metadata);

        await grain.ClearMetadataAsync();
        
        var newMetadata = await grain.GetMetadata();
        newMetadata.Should().NotBeEquivalentTo(metadata);
        newMetadata.Should().BeEmpty();
    }
}