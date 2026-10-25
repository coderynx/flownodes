using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal static class RevisionLog
{
    public static async Task RecordAsync(
        this IApplicationDbContext database,
        Revision revision,
        int keep,
        CancellationToken cancellationToken)
    {
        var expired = await database.Revisions
            .Where(older => older.Namespace == revision.Namespace && older.Kind == revision.Kind &&
                            older.Name == revision.Name)
            .OrderByDescending(older => older.Id)
            .Skip(Math.Max(keep - 1, 0))
            .ToListAsync(cancellationToken);
        if (expired.Count > 0)
        {
            database.Revisions.RemoveRange(expired);
        }

        database.Revisions.Add(revision);
    }
}
