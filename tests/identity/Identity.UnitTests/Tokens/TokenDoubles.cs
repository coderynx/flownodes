using Identity.Application.Tokens;
using Identity.Domain.Tokens;
using Identity.Domain.Users;

namespace Identity.UnitTests.Tokens;

internal static class TokenDoubles
{
    public const string Secret = "fnpat_the-secret";
    public static DateTimeOffset Now { get; } = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    public static IPersonalAccessTokenSecrets Secrets(string generated = "fnpat_generated")
    {
        var secrets = Substitute.For<IPersonalAccessTokenSecrets>();
        secrets.Generate().Returns(generated);
        secrets.Hash(Arg.Any<string>()).Returns(call => new PersonalAccessTokenSecrets().Hash(call.Arg<string>()));

        return secrets;
    }

    public static PersonalAccessToken TokenOf(User owner, DateTimeOffset? expiresAt = null) =>
        PersonalAccessToken.Create(
            owner.Id,
            "laptop",
            new PersonalAccessTokenSecrets().Hash(Secret),
            Now.AddDays(-1),
            expiresAt);
}
