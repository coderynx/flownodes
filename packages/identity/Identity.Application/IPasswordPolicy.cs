using Coderynx.Functional.Results.Errors;
using Identity.Domain.Users;

namespace Identity.Application;

internal interface IPasswordPolicy
{
    Task<Error?> ValidateAsync(User user, string password);
}
