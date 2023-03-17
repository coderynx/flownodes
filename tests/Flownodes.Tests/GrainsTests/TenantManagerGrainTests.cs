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
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    [Fact]
    public async Task CreateTenant_ShouldCreateTenant()
    {
        // Arrange.
        var tenantManager = _cluster.GrainFactory.GetGrain<ITenantManagerGrain>(_fixture.Create<string>());

        // Act.
        var tenant = await tenantManager.CreateTenantAsync("tenant_1");

        // Assert.
        tenant.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTenant_ShouldReturnTenant()
    {
        // Arrange.
        var tenantManager = _cluster.GrainFactory.GetGrain<ITenantManagerGrain>(_fixture.Create<string>());
        const string tenantId = "tenant_1";
        await tenantManager.CreateTenantAsync(tenantId);

        // Act.
        var tenant = await tenantManager.GetTenantAsync(tenantId);

        // Assert.
        tenant.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTenant_ShouldReturnNull_WhenTenantIsNotFound()
    {
        // Arrange.
        var tenantManager = _cluster.GrainFactory.GetGrain<ITenantManagerGrain>(_fixture.Create<string>());
        await tenantManager.CreateTenantAsync("tenant_1");

        // Act.
        var tenant = await tenantManager.GetTenantAsync("tenant_2");

        // Assert.
        tenant.Should().BeNull();
    }

    [Fact]
    public async Task GetTenants_ShouldReturnTenants()
    {
        // Arrange.
        var tenantManager = _cluster.GrainFactory.GetGrain<ITenantManagerGrain>(_fixture.Create<string>());
        await tenantManager.CreateTenantAsync("tenant_1");
        await tenantManager.CreateTenantAsync("tenant_2");

        // Act.
        var tenants = await tenantManager.GetTenantsAsync();

        // Assert.
        tenants.Should().HaveCount(2);
    }
}