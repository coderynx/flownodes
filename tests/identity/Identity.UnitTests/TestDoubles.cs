using Identity.Application;
using Identity.Application.Database;
using Identity.Domain.Namespaces;
using Identity.Domain.Tokens;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using MockQueryable.NSubstitute;

namespace Identity.UnitTests;

internal static class TestDoubles
{
    public static User Member(string username, string? displayName = null) =>
        User.Create(Username.Create(username), displayName);

    public static User Administrator(string username = "admin")
    {
        var user = Member(username);
        user.GrantAdministrator();

        return user;
    }

    public static User Disabled(User user)
    {
        user.Disable();

        return user;
    }

    public static IIdentityDbContext Database(
        IEnumerable<User> users,
        IEnumerable<PersonalAccessToken>? tokens = null,
        IEnumerable<KnownNamespace>? namespaces = null)
    {
        var database = Substitute.For<IIdentityDbContext>();
        var userSet = users.ToList().BuildMockDbSet();
        var tokenSet = (tokens ?? []).ToList().BuildMockDbSet();
        var namespaceSet = (namespaces ?? []).ToList().BuildMockDbSet();
        database.Users.Returns(userSet);
        database.PersonalAccessTokens.Returns(tokenSet);
        database.KnownNamespaces.Returns(namespaceSet);

        return database;
    }

    public static ICallerChecks Checks(IIdentityDbContext database)
    {
        var real = new CallerChecks(database);
        var checks = Substitute.For<ICallerChecks>();
        checks.RequireActiveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => real.RequireActiveAsync(call.ArgAt<Guid>(0), call.ArgAt<CancellationToken>(1)));
        checks.RequireAdministratorAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => real.RequireAdministratorAsync(call.ArgAt<Guid>(0), call.ArgAt<CancellationToken>(1)));
        checks.FindUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => real.FindUserAsync(call.ArgAt<Guid>(0), call.ArgAt<CancellationToken>(1)));
        checks.RequireAdministratorAndFindUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => real.RequireAdministratorAndFindUserAsync(
                call.ArgAt<Guid>(0),
                call.ArgAt<Guid>(1),
                call.ArgAt<CancellationToken>(2)));
        checks.AnotherActiveAdministratorExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => real.AnotherActiveAdministratorExistsAsync(
                call.ArgAt<Guid>(0),
                call.ArgAt<CancellationToken>(1)));

        return checks;
    }

    public static IReadOnlyList<object?> Published(IIdentityDbContext database) =>
        database.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IIdentityDbContext.PublishAsync))
            .Select(call => call.GetArguments()[0])
            .ToList();

    public static UserManager<User> UserManager(IPasswordHasher<User>? hasher = null)
    {
        var users = Substitute.For<UserManager<User>>(
            Substitute.For<IUserStore<User>>(),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
        users.PasswordHasher = hasher ?? Substitute.For<IPasswordHasher<User>>();

        return users;
    }

    public static IPasswordValidator<User> RejectingValidator(params string[] reasons)
    {
        var validator = Substitute.For<IPasswordValidator<User>>();
        var errors = reasons.Select(reason => new IdentityError { Description = reason }).ToArray();
        validator.ValidateAsync(Arg.Any<UserManager<User>>(), Arg.Any<User>(), Arg.Any<string?>())
            .Returns(IdentityResult.Failed(errors));

        return validator;
    }

    public static IPasswordValidator<User> AcceptingValidator()
    {
        var validator = Substitute.For<IPasswordValidator<User>>();
        validator.ValidateAsync(Arg.Any<UserManager<User>>(), Arg.Any<User>(), Arg.Any<string?>())
            .Returns(IdentityResult.Success);

        return validator;
    }

    public static TimeProvider Clock(DateTimeOffset now)
    {
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(now);

        return time;
    }
}
