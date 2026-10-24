using Identity.Application.Database;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using MockQueryable.NSubstitute;

namespace Identity.UnitTests.Setup;

internal static class SetupDoubles
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

    public static User ExistingUser() => User.Create(Username.Create("alice"), null);
}
