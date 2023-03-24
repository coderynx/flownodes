using Flownodes.Shared.Users;

namespace Flownodes.Worker.Users.Surrogates;

[GenerateSerializer]
internal struct ApplicationUserSurrogate
{
    public ApplicationUserSurrogate()
    {
    }

    [Id(0)] public string Id { get; set; } = default!;
    [Id(1)] public string? UserName { get; set; }
    [Id(2)] public string? NormalizedUserName { get; set; }
    [Id(3)] public string? Email { get; set; }
    [Id(4)] public string? NormalizedEmail { get; set; }
    [Id(5)] public bool EmailConfirmed { get; set; }
    [Id(6)] public string? PasswordHash { get; set; }
    [Id(7)] public string? SecurityStamp { get; set; }
    [Id(8)] public string? ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
    [Id(9)] public string? PhoneNumber { get; set; }
    [Id(10)] public bool PhoneNumberConfirmed { get; set; }
    [Id(11)] public bool TwoFactorEnabled { get; set; }
    [Id(12)] public DateTimeOffset? LockoutEnd { get; set; }
    [Id(13)] public bool LockoutEnabled { get; set; }

    [Id(14)] public int AccessFailedCount { get; set; }

    public override string ToString()
    {
        return UserName ?? string.Empty;
    }
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