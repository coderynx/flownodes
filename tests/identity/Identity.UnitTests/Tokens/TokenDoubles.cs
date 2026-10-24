using Identity.Application.Database;
using Identity.Application.Tokens;
using Identity.Domain.Tokens;
using Identity.Domain.Users;
using MockQueryable.NSubstitute;

namespace Identity.UnitTests.Tokens;

internal static class TokenDoubles
{
    public const string Secret = "fnpat_the-secret";
    public static DateTimeOffset Now { get; } = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    public static IIdentityDbContext Database(IEnumerable<User> users, IEnumerable<PersonalAccessToken> tokens)
    {
        var database = Substitute.For<IIdentityDbContext>();
        var userSet = users.ToList().BuildMockDbSet();
        var tokenSet = tokens.ToList().BuildMockDbSet();
        database.Users.Returns(userSet);
        database.PersonalAccessTokens.Returns(tokenSet);

        return database;
    }

    public static TimeProvider Clock()
    {
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);

        return time;
    }

    public static User ActiveUser() => User.Create(Username.Create("alice"), null);

    public static User DisabledUser()
    {
        var user = User.Create(Username.Create("bob"), null);
        user.Disable();

        return user;
    }

    public static PersonalAccessToken TokenOf(User owner, DateTimeOffset? expiresAt = null) =>
        PersonalAccessToken.Create(
            owner.Id,
            "laptop",
            PersonalAccessTokenSecrets.Hash(Secret),
            Now.AddDays(-1),
            expiresAt);
}
