using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Shared.Interfaces;
using Flownodes.Tests.Fixtures;
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
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    [Fact]
    public async Task GetClusterInformation_ShouldReturnClusterInformation()
    {
        // Arrange.
        var clusterGrain = _cluster.GrainFactory.GetGrain<IClusterGrain>(0);
        
        // Act.
        var clusterInformation = await clusterGrain.GetClusterInformation();
        
        // Assert.
        clusterInformation.Should().NotBeNull();
    }
}