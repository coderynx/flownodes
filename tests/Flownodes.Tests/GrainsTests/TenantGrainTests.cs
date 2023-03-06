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
        _cluster = fixture.Cluster;
        _fixture = new Fixture();
    }

    [Fact]
    public async Task UpdateConfiguration_ShouldUpdateConfiguration()
    {
        var grain = _cluster.GrainFactory.GetGrain<ITenantGrain>(_fixture.Create<string>());

        await grain.UpdateMetadataAsync(_fixture.Create<Dictionary<string, string?>>());

        var metadata = await grain.GetMetadata();
        metadata.Should().NotBeNull();
    }
}