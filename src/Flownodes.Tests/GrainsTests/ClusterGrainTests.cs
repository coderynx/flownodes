using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Shared.Interfaces;
using Flownodes.Tests.Configuration;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class ClusterGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public ClusterGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster;
        _fixture = new Fixture();
    }

    [Fact]
    public async Task ShouldGetClusterInformation()
    {
        var clusterGrain = _cluster.GrainFactory.GetGrain<IClusterGrain>(0);
        var clusterInformation = await clusterGrain.GetClusterInformation();
        clusterInformation.Should().NotBeNull();
    }

}