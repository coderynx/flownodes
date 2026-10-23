namespace Registry.Application.Database;

/// <summary>
/// The Registry's database as its command handlers see it: the unit of work an apply commits through.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Commits every change tracked since the last save in one transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancels the commit.</param>
    /// <returns>The number of rows written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
