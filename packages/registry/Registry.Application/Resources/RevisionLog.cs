using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Registry.Application.Database;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal sealed class RevisionLog(IApplicationDbContext database, IOptions<RevisionRetention> retention) : IRevisionLog
{
    public async Task RecordAsync(Revision revision, CancellationToken cancellationToken)
    {
        var expired = await database.Revisions
            .Where(older => older.Namespace == revision.Namespace && older.Kind == revision.Kind &&
                            older.Name == revision.Name)
            .OrderByDescending(older => older.Id)
            .Skip(Math.Max(retention.Value.Keep - 1, 0))
            .ToListAsync(cancellationToken);
        if (expired.Count > 0)
        {
            database.Revisions.RemoveRange(expired);
        }

        database.Revisions.Add(revision);
    }
}
