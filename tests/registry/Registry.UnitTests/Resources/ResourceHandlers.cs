using Microsoft.Extensions.Options;
using Registry.Application.Resources;

namespace Registry.UnitTests.Resources;

internal static class ResourceHandlers
{
    public static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    public static TimeProvider Time()
    {
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);

        return time;
    }

    public static IOptions<RevisionRetention> Retention(int keep = 50) =>
        Options.Create(new RevisionRetention { Keep = keep });
}
