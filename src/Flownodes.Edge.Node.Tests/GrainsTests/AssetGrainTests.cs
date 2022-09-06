using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Edge.Core.Resources;
using Flownodes.Edge.Node.Tests.Configuration;
using FluentAssertions;
using Orleans;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Edge.Node.Tests.GrainsTests;

[Collection(nameof(ClusterFixture))]
public class AssetGrainTests : IClassFixture<ClusterFixture>
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public AssetGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster;
        _fixture = new Fixture();
    }

    [Fact]
    public void ShouldActivate()
    {
        var grain = _cluster.GrainFactory.GetGrain<IAssetGrain>(_fixture.Create<string>());
        grain.GetPrimaryKeyString().Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldUpdate()
    {
        var grain = _cluster.GrainFactory.GetGrain<IAssetGrain>(_fixture.Create<string>());

        var data = new { Test = "Hello" };
        await grain.UpdateAsync(data);

        var result = await grain.QueryData("$.Test");
        Assert.Equal(data.Test, result.ToString());
    }
}