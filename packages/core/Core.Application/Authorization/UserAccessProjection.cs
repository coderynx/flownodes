using Core.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Authorization;

internal sealed class UserAccessProjection(IUserAccessSource source) : IUserAccessProjection
{
    public async Task ApplyAsync(Guid userId, Action<UserAccess> change, CancellationToken cancellationToken = default)
    {
        var user = await source.UserAccess.Include(candidate => candidate.Grants)
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            user = UserAccess.Create(userId);
            source.UserAccess.Add(user);
        }

        change(user);

        await source.SaveChangesAsync(cancellationToken);
    }
}
