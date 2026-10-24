using Identity.Domain.Users;
using Xunit;

namespace Identity.UnitTests.Domain.Users;

public sealed class UserTests
{
    private const string Default = "default";
    private const string Plant = "plant";

    [Fact]
    public void Create_ShouldStartWithNoPasswordNoRolesAndSequenceZero()
    {
        // Act
        var user = User.Create(Username.Create("alice"), "Alice A.");

        // Assert
        user.Id.ShouldNotBe(Guid.Empty);
        user.Username.Value.ShouldBe("alice");
        user.DisplayName.ShouldBe("Alice A.");
        user.PasswordHash.ShouldBeNull();
        user.AccessFailedCount.ShouldBe(0);
        user.LockoutEnd.ShouldBeNull();
        user.IsDisabled.ShouldBeFalse();
        user.IsAdministrator.ShouldBeFalse();
        user.RoleGrants.ShouldBeEmpty();
        user.Seq.ShouldBe(0);
    }

    [Fact]
    public void Create_ShouldGiveEachUserItsOwnId()
    {
        // Arrange
        var first = User.Create(Username.Create("alice"), null);

        // Act
        var second = User.Create(Username.Create("alice"), null);

        // Assert
        second.Id.ShouldNotBe(first.Id);
    }

    [Fact]
    public void Create_ShouldTrimTheDisplayName_WhenItHasSurroundingWhitespace()
    {
        // Act
        var user = User.Create(Username.Create("alice"), "  Alice A.  ");

        // Assert
        user.DisplayName.ShouldBe("Alice A.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldLeaveTheDisplayNameUnset_WhenItIsBlank(string? displayName)
    {
        // Act
        var user = User.Create(Username.Create("alice"), displayName);

        // Assert
        user.DisplayName.ShouldBeNull();
    }

    [Fact]
    public void Rename_ShouldChangeTheUsernameAndAdvanceTheSequence_WhenTheUsernameDiffers()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);

        // Act
        var renamed = user.Rename(Username.Create("alicia"));

        // Assert
        renamed.ShouldBeTrue();
        user.Username.Value.ShouldBe("alicia");
        user.Seq.ShouldBe(1);
    }

    [Fact]
    public void Rename_ShouldChangeTheUsername_WhenOnlyTheCaseDiffers()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);

        // Act
        var renamed = user.Rename(Username.Create("Alice"));

        // Assert
        renamed.ShouldBeTrue();
        user.Username.Value.ShouldBe("Alice");
        user.Seq.ShouldBe(1);
    }

    [Fact]
    public void Rename_ShouldChangeNothing_WhenTheUsernameIsTheSame()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);

        // Act
        var renamed = user.Rename(Username.Create("alice"));

        // Assert
        renamed.ShouldBeFalse();
        user.Seq.ShouldBe(0);
    }

    [Fact]
    public void ChangeDisplayName_ShouldTrimTheNameWithoutAdvancingTheSequence()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);

        // Act
        user.ChangeDisplayName(" Alice A. ");

        // Assert
        user.DisplayName.ShouldBe("Alice A.");
        user.Seq.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ChangeDisplayName_ShouldClearTheName_WhenItIsBlank(string? displayName)
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), "Alice A.");

        // Act
        user.ChangeDisplayName(displayName);

        // Assert
        user.DisplayName.ShouldBeNull();
    }

    [Fact]
    public void SetPasswordHash_ShouldReplaceTheHash()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.SetPasswordHash("old");

        // Act
        user.SetPasswordHash("new");

        // Assert
        user.PasswordHash.ShouldBe("new");
        user.Seq.ShouldBe(0);
    }

    [Fact]
    public void RecordFailedAccess_ShouldReturnTheRunningCount_WhenAttemptsKeepFailing()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.RecordFailedAccess();

        // Act
        var count = user.RecordFailedAccess();

        // Assert
        count.ShouldBe(2);
        user.AccessFailedCount.ShouldBe(2);
    }

    [Fact]
    public void ResetFailedAccess_ShouldForgetTheFailedAttempts()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.RecordFailedAccess();
        user.RecordFailedAccess();

        // Act
        user.ResetFailedAccess();

        // Assert
        user.AccessFailedCount.ShouldBe(0);
    }

    [Fact]
    public void LockOutUntil_ShouldSetTheLockoutEnd()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        var end = new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

        // Act
        user.LockOutUntil(end);

        // Assert
        user.LockoutEnd.ShouldBe(end);
    }

    [Fact]
    public void LockOutUntil_ShouldClearTheLockout_WhenTheEndIsNull()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.LockOutUntil(DateTimeOffset.UnixEpoch);

        // Act
        user.LockOutUntil(null);

        // Assert
        user.LockoutEnd.ShouldBeNull();
    }

    [Fact]
    public void Disable_ShouldDisableTheUserAndAdvanceTheSequence_WhenTheUserIsEnabled()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);

        // Act
        var changed = user.Disable();

        // Assert
        changed.ShouldBeTrue();
        user.IsDisabled.ShouldBeTrue();
        user.Seq.ShouldBe(1);
    }

    [Fact]
    public void Disable_ShouldChangeNothing_WhenTheUserIsAlreadyDisabled()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.Disable();

        // Act
        var changed = user.Disable();

        // Assert
        changed.ShouldBeFalse();
        user.IsDisabled.ShouldBeTrue();
        user.Seq.ShouldBe(1);
    }

    [Fact]
    public void Enable_ShouldEnableTheUserAndAdvanceTheSequence_WhenTheUserIsDisabled()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.Disable();

        // Act
        var changed = user.Enable();

        // Assert
        changed.ShouldBeTrue();
        user.IsDisabled.ShouldBeFalse();
        user.Seq.ShouldBe(2);
    }

    [Fact]
    public void Enable_ShouldChangeNothing_WhenTheUserIsAlreadyEnabled()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);

        // Act
        var changed = user.Enable();

        // Assert
        changed.ShouldBeFalse();
        user.IsDisabled.ShouldBeFalse();
        user.Seq.ShouldBe(0);
    }

    [Fact]
    public void GrantAdministrator_ShouldMakeTheUserAnAdministratorAndAdvanceTheSequence_WhenTheUserIsNotOne()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);

        // Act
        var changed = user.GrantAdministrator();

        // Assert
        changed.ShouldBeTrue();
        user.IsAdministrator.ShouldBeTrue();
        user.Seq.ShouldBe(1);
    }

    [Fact]
    public void GrantAdministrator_ShouldChangeNothing_WhenTheUserIsAlreadyAnAdministrator()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.GrantAdministrator();

        // Act
        var changed = user.GrantAdministrator();

        // Assert
        changed.ShouldBeFalse();
        user.IsAdministrator.ShouldBeTrue();
        user.Seq.ShouldBe(1);
    }

    [Fact]
    public void RevokeAdministrator_ShouldStopTheUserBeingAnAdministratorAndAdvanceTheSequence_WhenTheUserIsOne()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.GrantAdministrator();

        // Act
        var changed = user.RevokeAdministrator();

        // Assert
        changed.ShouldBeTrue();
        user.IsAdministrator.ShouldBeFalse();
        user.Seq.ShouldBe(2);
    }

    [Fact]
    public void RevokeAdministrator_ShouldChangeNothing_WhenTheUserIsNotAnAdministrator()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);

        // Act
        var changed = user.RevokeAdministrator();

        // Assert
        changed.ShouldBeFalse();
        user.IsAdministrator.ShouldBeFalse();
        user.Seq.ShouldBe(0);
    }

    [Fact]
    public void Grant_ShouldAddARoleGrantAndAdvanceTheSequence_WhenTheUserDoesNotHoldTheRole()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);

        // Act
        var changed = user.Grant(Default, Role.Engineer);

        // Assert
        changed.ShouldBeTrue();
        var grant = user.RoleGrants.ShouldHaveSingleItem();
        grant.UserId.ShouldBe(user.Id);
        grant.Namespace.ShouldBe(Default);
        grant.Role.ShouldBe(Role.Engineer);
        user.Seq.ShouldBe(1);
    }

    [Fact]
    public void Grant_ShouldChangeNothing_WhenTheUserAlreadyHoldsTheRoleInTheNamespace()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.Grant(Default, Role.Engineer);

        // Act
        var changed = user.Grant(Default, Role.Engineer);

        // Assert
        changed.ShouldBeFalse();
        user.RoleGrants.Count.ShouldBe(1);
        user.Seq.ShouldBe(1);
    }

    [Theory]
    [InlineData(Default, Role.Operator)]
    [InlineData(Plant, Role.Engineer)]
    public void Grant_ShouldAddASecondRoleGrant_WhenTheRoleOrTheNamespaceDiffers(string @namespace, Role role)
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.Grant(Default, Role.Engineer);

        // Act
        var changed = user.Grant(@namespace, role);

        // Assert
        changed.ShouldBeTrue();
        user.RoleGrants.Count.ShouldBe(2);
        user.Seq.ShouldBe(2);
    }

    [Fact]
    public void Revoke_ShouldRemoveTheRoleGrantAndAdvanceTheSequence_WhenTheUserHoldsTheRole()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.Grant(Default, Role.Engineer);
        user.Grant(Default, Role.Operator);

        // Act
        var changed = user.Revoke(Default, Role.Engineer);

        // Assert
        changed.ShouldBeTrue();
        user.RoleGrants.ShouldHaveSingleItem().Role.ShouldBe(Role.Operator);
        user.Seq.ShouldBe(3);
    }

    [Theory]
    [InlineData(Plant, Role.Engineer)]
    [InlineData(Default, Role.Operator)]
    public void Revoke_ShouldChangeNothing_WhenTheUserDoesNotHoldTheRoleInTheNamespace(string @namespace, Role role)
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.Grant(Default, Role.Engineer);

        // Act
        var changed = user.Revoke(@namespace, role);

        // Assert
        changed.ShouldBeFalse();
        user.RoleGrants.Count.ShouldBe(1);
        user.Seq.ShouldBe(1);
    }

    [Fact]
    public void RevokeAllIn_ShouldRevokeEveryRoleInTheNamespaceWithOneSequenceNumberEach()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.Grant(Plant, Role.Operator);
        user.Grant(Default, Role.Engineer);
        user.Grant(Plant, Role.Engineer);

        // Act
        var revoked = user.RevokeAllIn(Plant);

        // Assert
        revoked.ShouldBe([new RoleRevocation(Role.Operator, 4), new RoleRevocation(Role.Engineer, 5)]);
        user.RoleGrants.ShouldHaveSingleItem().Namespace.ShouldBe(Default);
        user.Seq.ShouldBe(5);
    }

    [Fact]
    public void RevokeAllIn_ShouldRevokeNothing_WhenTheUserHoldsNoRoleInTheNamespace()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.Grant(Default, Role.Engineer);

        // Act
        var revoked = user.RevokeAllIn(Plant);

        // Assert
        revoked.ShouldBeEmpty();
        user.RoleGrants.Count.ShouldBe(1);
        user.Seq.ShouldBe(1);
    }

    [Fact]
    public void Delete_ShouldAdvanceTheSequence()
    {
        // Arrange
        var user = User.Create(Username.Create("alice"), null);
        user.Disable();

        // Act
        user.Delete();

        // Assert
        user.Seq.ShouldBe(2);
    }
}
