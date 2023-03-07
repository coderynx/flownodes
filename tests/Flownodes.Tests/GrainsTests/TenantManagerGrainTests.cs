using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Shared.Interfaces;
using Flownodes.Tests.Fixtures;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class TenantManagerGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public TenantManagerGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster;
        _fixture = new Fixture();
    }

    [Fact]
    public async Task CreateTenant_ShouldCreateTenant()
    {
        var tenantManager = _cluster.GrainFactory.GetGrain<ITenantManagerGrain>(_fixture.Create<string>());

        var tenant = await tenantManager.CreateTenantAsync(_fixture.Create<string>());
        tenant.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTenant_ShouldGetTenant()
    {
        var tenantManager = _cluster.GrainFactory.GetGrain<ITenantManagerGrain>(_fixture.Create<string>());

        var tenantId = _fixture.Create<string>();
        await tenantManager.CreateTenantAsync(tenantId);
        var tenant = await tenantManager.GetTenantAsync(tenantId);

        tenant.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTenants_ShouldGetTenants()
    {
        var tenantManager = _cluster.GrainFactory.GetGrain<ITenantManagerGrain>(_fixture.Create<string>());

        await tenantManager.CreateTenantAsync(_fixture.Create<string>());
        await tenantManager.CreateTenantAsync(_fixture.Create<string>());

        var tenants = await tenantManager.GetTenantsAsync();

        tenants.Should().HaveCount(2);
    }
}