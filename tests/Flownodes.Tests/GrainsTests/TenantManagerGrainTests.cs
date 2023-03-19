using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk;
using Flownodes.Shared.Tenanting;
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

    private FlownodesId NewFlownodesId => new(FlownodesObject.TenantManager, _fixture.Create<string>());

    private ITenantManagerGrain NewTenantManagerGrain =>
        _cluster.GrainFactory.GetGrain<ITenantManagerGrain>(NewFlownodesId);

    [Fact]
    public async Task CreateTenant_ShouldCreateTenant()
    {
        // Arrange.
        var tenantManager = NewTenantManagerGrain;

        // Act.
        var tenant = await tenantManager.CreateTenantAsync("tenant_1");

        // Assert.
        tenant.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTenant_ShouldReturnTenant()
    {
        // Arrange.
        var tenantManager = NewTenantManagerGrain;
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
        var tenantManager = NewTenantManagerGrain;
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
        var tenantManager = NewTenantManagerGrain;
        await tenantManager.CreateTenantAsync("tenant_1");
        await tenantManager.CreateTenantAsync("tenant_2");

        // Act.
        var tenants = await tenantManager.GetTenantsAsync();

        // Assert.
        tenants.Should().HaveCount(2);
    }
}