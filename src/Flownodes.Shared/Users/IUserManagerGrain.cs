namespace Flownodes.Shared.Users;

public interface IUserManagerGrain : IGrainWithStringKey
{
    Task CreateUserAsync(ApplicationUser user);
    Task UpdateUserAsync(ApplicationUser user);
    Task DeleteUserAsync(ApplicationUser user);
    ValueTask<ApplicationUser?> FindByIdAsync(string userId);
    ValueTask<ApplicationUser?> FindByNormalizedUsernameAsync(string normalizedUsername);
}