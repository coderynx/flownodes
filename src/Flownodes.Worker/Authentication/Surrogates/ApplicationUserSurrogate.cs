using Flownodes.Shared.Authentication.Models;

namespace Flownodes.Worker.Authentication.Surrogates;

[GenerateSerializer]
internal struct ApplicationUserSurrogate
{
    public ApplicationUserSurrogate()
    {
    }

    [Id(0)] public string Id { get; init; } = default!;
    [Id(1)] public string? UserName { get; init; }
    [Id(2)] public string? NormalizedUserName { get; init; }
    [Id(3)] public string? Email { get; init; }
    [Id(4)] public string? NormalizedEmail { get; init; }
    [Id(5)] public bool EmailConfirmed { get; init; }
    [Id(6)] public string? PasswordHash { get; init; }
    [Id(7)] public string? SecurityStamp { get; init; }
    [Id(8)] public string? ConcurrencyStamp { get; init; }
    [Id(9)] public string? PhoneNumber { get; init; }
    [Id(10)] public bool PhoneNumberConfirmed { get; init; }
    [Id(11)] public bool TwoFactorEnabled { get; init; }
    [Id(12)] public DateTimeOffset? LockoutEnd { get; init; }
    [Id(13)] public bool LockoutEnabled { get; init; }
    [Id(14)] public int AccessFailedCount { get; init; }
}

[RegisterConverter]
internal class ApplicationUserSurrogateConverter : IConverter<ApplicationUser, ApplicationUserSurrogate>
{
    public ApplicationUser ConvertFromSurrogate(in ApplicationUserSurrogate surrogate)
    {
        return new ApplicationUser
        {
            Id = surrogate.Id,
            UserName = surrogate.UserName,
            NormalizedUserName = surrogate.NormalizedUserName,
            Email = surrogate.Email,
            NormalizedEmail = surrogate.NormalizedEmail,
            EmailConfirmed = surrogate.EmailConfirmed,
            PasswordHash = surrogate.PasswordHash,
            SecurityStamp = surrogate.SecurityStamp,
            ConcurrencyStamp = surrogate.ConcurrencyStamp,
            PhoneNumber = surrogate.PhoneNumber,
            PhoneNumberConfirmed = surrogate.PhoneNumberConfirmed,
            TwoFactorEnabled = surrogate.TwoFactorEnabled,
            LockoutEnd = surrogate.LockoutEnd,
            LockoutEnabled = surrogate.LockoutEnabled,
            AccessFailedCount = surrogate.AccessFailedCount
        };
    }

    public ApplicationUserSurrogate ConvertToSurrogate(in ApplicationUser value)
    {
        return new ApplicationUserSurrogate
        {
            Id = value.Id,
            UserName = value.UserName,
            NormalizedUserName = value.NormalizedUserName,
            Email = value.Email,
            NormalizedEmail = value.NormalizedEmail,
            EmailConfirmed = value.EmailConfirmed,
            PasswordHash = value.PasswordHash,
            SecurityStamp = value.SecurityStamp,
            ConcurrencyStamp = value.ConcurrencyStamp,
            PhoneNumber = value.PhoneNumber,
            PhoneNumberConfirmed = value.PhoneNumberConfirmed,
            TwoFactorEnabled = value.TwoFactorEnabled,
            LockoutEnd = value.LockoutEnd,
            LockoutEnabled = value.LockoutEnabled,
            AccessFailedCount = value.AccessFailedCount
        };
    }
}