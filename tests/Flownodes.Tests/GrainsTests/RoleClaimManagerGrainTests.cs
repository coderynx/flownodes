using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Authentication;
using Flownodes.Shared.Authentication.Models;
using Flownodes.Tests.Fixtures;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class RoleClaimManagerGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public RoleClaimManagerGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private FlownodesId NewFlownodesId => new(FlownodesEntity.RoleClaimManager, _fixture.Create<string>());

    private IRoleClaimManagerGrain NewRoleClaimManager =>
        _cluster.GrainFactory.GetGrain<IRoleClaimManagerGrain>(NewFlownodesId);

    [Fact]
    public async Task AddRole_ShouldAddClaim()
    {
        // Arrange.
        var grain = NewRoleClaimManager;

        // Act.
        var role = _fixture.Create<ApplicationRole>();
        await grain.AddRoleAsync(role);

        // Assert.
        var addedRole = await grain.GetRoleById(role.Id);
        addedRole.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateRole_ShouldUpdateRole()
    {
        // Arrange.
        var grain = NewRoleClaimManager;
        var role = _fixture.Create<ApplicationRole>();
        await grain.AddRoleAsync(role);
        role.Name = "NewName";

        // Act.
        await grain.UpdateRoleAsync(role);

        // Assert.
        var updatedRole = await grain.GetRoleById(role.Id);
        updatedRole!.Name.Should().Be("NewName");
    }

    [Fact]
    public async Task DeleteRole_ShouldDeleteRole()
    {
        // Arrange.
        var grain = NewRoleClaimManager;
        var role = _fixture.Create<ApplicationRole>();
        await grain.AddRoleAsync(role);

        // Act.
        await grain.DeleteRoleAsync(role.Id);

        // Assert.
        var deletedRole = await grain.GetRoleById(role.Id);
        deletedRole.Should().BeNull();
    }

    [Fact]
    public async Task FindRoleByNormalizedName_ShouldReturnRole()
    {
        // Arrange.
        var grain = NewRoleClaimManager;
        var role = _fixture.Create<ApplicationRole>();
        await grain.AddRoleAsync(role);

        // Act.
        var foundRole = await grain.FindByNormalizedNameAsync(role.NormalizedName!);

        // Assert.
        foundRole.Should().NotBeNull();
    }

    [Fact]
    public async Task AddRoleClaim_ShouldAddRoleClaim()
    {
        // Arrange.
        var grain = NewRoleClaimManager;
        var roleClaim = _fixture.Create<ApplicationRoleClaim>();

        // Act.
        await grain.AddRoleClaimAsync("role", roleClaim);

        // Assert.
        var claims = await grain.GetApplicationRoleClaims("role");
        claims.Should().ContainEquivalentOf(roleClaim);
    }

    [Fact]
    public async Task DeleteRoleClaim_ShouldDeleteRoleClaim()
    {
        // Arrange.
        var grain = NewRoleClaimManager;
        var roleClaim = _fixture.Create<ApplicationRoleClaim>();
        await grain.AddRoleClaimAsync("role", roleClaim);

        // Act.
        await grain.DeleteRoleClaimAsync("role", roleClaim.ClaimType!, roleClaim.ClaimValue!);

        // Assert.
        var claims = await grain.GetApplicationRoleClaims("role");
        claims.Should().BeEmpty();
    }
}