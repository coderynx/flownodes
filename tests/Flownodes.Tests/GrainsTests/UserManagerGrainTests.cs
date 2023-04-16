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
public class UserManagerGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public UserManagerGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private EntityId NewEntityId => new(Entity.UserManager, _fixture.Create<string>());

    private IUserManagerGrain NewUserManagerGrain =>
        _cluster.GrainFactory.GetGrain<IUserManagerGrain>(NewEntityId);

    [Fact]
    public async Task CreateUser_ShouldCreateUser()
    {
        // Arrange.
        var userManager = NewUserManagerGrain;
        var user = _fixture.Create<ApplicationUser>();

        // Act.
        await userManager.CreateUserAsync(user);

        // Assert.
        var createdUser = await userManager.FindByIdAsync(user.Id);
        createdUser.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateUser_ShouldUpdateUser()
    {
        // Arrange.
        var userManager = NewUserManagerGrain;
        var user = _fixture.Create<ApplicationUser>();
        await userManager.CreateUserAsync(user);

        // Act.
        user.UserName = "user";
        await userManager.UpdateUserAsync(user);

        // Assert.
        var updatedUser = await userManager.FindByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.UserName.Should().Be("user");
    }

    [Fact]
    public async Task DeleteUser_ShouldDeleteUser()
    {
        // Arrange.
        var userManager = NewUserManagerGrain;
        var user = _fixture.Create<ApplicationUser>();
        await userManager.CreateUserAsync(user);

        // Act.
        await userManager.DeleteUserAsync(user);

        var deletedUser = await userManager.FindByIdAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task FindByNormalizedUsername_ShouldFindUser()
    {
        // Arrange.
        var userManager = NewUserManagerGrain;
        var user = _fixture.Create<ApplicationUser>();
        user.NormalizedUserName = "USERNAME";
        await userManager.CreateUserAsync(user);

        // Act.
        var foundUser = await userManager.FindByNormalizedUsernameAsync(user.NormalizedUserName);

        // Assert.
        foundUser.Should().NotBeNull();
    }
}