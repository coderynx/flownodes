using Coderynx.Functional.Results.Errors;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application;

internal sealed class PasswordPolicy(UserManager<User> users) : IPasswordPolicy
{
    public async Task<Error?> ValidateAsync(User user, string password)
    {
        foreach (var validator in users.PasswordValidators)
        {
            var result = await validator.ValidateAsync(users, user, password);
            if (!result.Succeeded)
            {
                return Errors.InvalidPassword(result.Errors.Select(error => error.Description));
            }
        }

        return null;
    }
}
