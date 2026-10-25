namespace Registry.Application;

internal static class Paging
{
    private const int MaxLimit = 200;

    public static IEnumerable<T> Page<T>(this IEnumerable<T> items, int offset, int limit) =>
        items.Skip(Offset(offset)).Take(Limit(limit));

    public static IQueryable<T> Page<T>(this IQueryable<T> items, int offset, int limit) =>
        items.Skip(Offset(offset)).Take(Limit(limit));

    private static int Offset(int offset) => Math.Max(offset, 0);
    private static int Limit(int limit) => Math.Clamp(limit, 1, MaxLimit);
}
