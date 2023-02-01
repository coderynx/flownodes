using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
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

    private TenantConfiguration ProvideTenantConfiguration()
    {
        return _fixture.Create<TenantConfiguration>();
    }
    
    [Fact]
    public async Task UpdateConfiguration_ShouldUpdateConfiguration()
    {
        var grain = _cluster.GrainFactory.GetGrain<ITenantGrain>(_fixture.Create<string>());

        var configuration = ProvideTenantConfiguration();
        await grain.UpdateConfigurationAsync(configuration);

        var newConfiguration = await grain.GetConfiguration();
        newConfiguration.Should().BeEquivalentTo(configuration);
    }
}