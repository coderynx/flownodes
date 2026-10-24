using Identity.Application.Database;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using MockQueryable.NSubstitute;

namespace Identity.UnitTests.SignIn;

internal static class SignInDoubles
{
    public static IIdentityDbContext Database(IEnumerable<User> users)
    {
        var database = Substitute.For<IIdentityDbContext>();
        var userSet = users.ToList().BuildMockDbSet();
        database.Users.Returns(userSet);

        return database;
    }

    public static UserManager<User> UserManager() =>
        Substitute.For<UserManager<User>>(
            Substitute.For<IUserStore<User>>(),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

    public static User ActiveUser() => User.Create(Username.Create("alice"), null);

    public static User DisabledUser()
    {
        var user = User.Create(Username.Create("bob"), null);
        user.Disable();

        return user;
    }
}
