using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.Database;
using Identity.Application.Security;
using Identity.Domain.Namespaces;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Setup.CompleteSetup;

/// <summary>
/// Creates the installation's first User, who becomes Administrator and holds Engineer and Operator in the
/// <c>default</c> Namespace. It works only while no User exists and only with the setup token from the Silo's log.
/// </summary>
/// <param name="SetupToken">The one-time token the Silo logged.</param>
/// <param name="Username">The username, validated by the handler.</param>
/// <param name="DisplayName">The optional display name.</param>
/// <param name="Password">The password, validated against the password policy.</param>
public sealed record Command(string SetupToken, string Username, string? DisplayName, string Password)
    : IRequest<Result<User>>;

internal sealed class Handler(IIdentityDbContext database, UserManager<User> users, ISetupToken setupToken)
    : IRequestHandler<Command, Result<User>>
{
    public async Task<Result<User>> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        if (await database.Users.AnyAsync(cancellationToken))
        {
            return Errors.SetupDone();
        }

        if (!setupToken.Matches(request.SetupToken))
        {
            return Errors.InvalidSetupToken();
        }

        if (!Username.TryCreate(request.Username, out var username))
        {
            return Errors.InvalidUsername(request.Username);
        }

        var user = User.Create(username, request.DisplayName);
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return Errors.InvalidPassword(created.Errors.Select(error => error.Description));
        }

        user.GrantAdministrator();
        await database.PublishAsync(user.ToAdministratorGranted());
        user.Grant(KnownNamespace.Default, Role.Engineer);
        await database.PublishAsync(user.ToRoleGranted(KnownNamespace.Default, Role.Engineer));
        user.Grant(KnownNamespace.Default, Role.Operator);
        await database.PublishAsync(user.ToRoleGranted(KnownNamespace.Default, Role.Operator));
        await database.SaveChangesAsync(cancellationToken);

        setupToken.Invalidate();

        return Result.Created(user);
    }
}
