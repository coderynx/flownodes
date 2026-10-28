using Core.Domain.Scheduling;
using Xunit;

namespace Engine.UnitTests.Domain;

public sealed class CronScheduleTests
{
    private static readonly TimeZoneInfo Rome = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    [Theory]
    [InlineData("0 2 * * *", "2026-01-01T00:00:00Z", "2026-01-01T02:00:00Z")]
    [InlineData("*/15 * * * *", "2026-01-01T00:07:00Z", "2026-01-01T00:15:00Z")]
    [InlineData("0 9-17/4 * * *", "2026-01-01T10:00:00Z", "2026-01-01T13:00:00Z")]
    [InlineData("30 8 * * 1-5", "2026-01-02T09:00:00Z", "2026-01-05T08:30:00Z")]
    [InlineData("0 0 29 2 *", "2026-01-01T00:00:00Z", "2028-02-29T00:00:00Z")]
    [InlineData("0 0 1 * 0", "2026-01-02T00:00:00Z", "2026-01-04T00:00:00Z")]
    [InlineData("0 0 * * 7", "2026-01-02T00:00:00Z", "2026-01-04T00:00:00Z")]
    [InlineData("*/10 * * * * *", "2026-01-01T00:00:05Z", "2026-01-01T00:00:10Z")]
    [InlineData("0 2 * * *", "2026-01-01T02:00:00Z", "2026-01-02T02:00:00Z")]
    public void Next_ShouldReturnTheNextMatchingTime_WhenTheZoneIsUtc(string cron, string after, string expected)
    {
        // Arrange
        CronSchedule.TryCreate(cron, out var schedule).ShouldBeTrue();

        // Act
        var next = schedule.Next(At(after), TimeZoneInfo.Utc);

        // Assert
        next.ShouldBe(At(expected));
    }

    [Fact]
    public void Next_ShouldFollowTheZone_WhenTheZoneIsNotUtc()
    {
        // Arrange
        CronSchedule.TryCreate("0 2 * * *", out var schedule).ShouldBeTrue();

        // Act
        var next = schedule.Next(At("2026-06-30T12:00:00Z"), Rome);

        // Assert
        next.ShouldBe(At("2026-07-01T00:00:00Z"));
    }

    [Fact]
    public void Next_ShouldFireAtTheNextValidTime_WhenTheTimeFallsInADaylightSavingGap()
    {
        // Arrange
        CronSchedule.TryCreate("30 2 * * *", out var schedule).ShouldBeTrue();

        // Act
        var next = schedule.Next(At("2026-03-28T12:00:00Z"), Rome);

        // Assert
        next.ShouldBe(At("2026-03-29T03:00:00+02:00"));
    }

    [Fact]
    public void Next_ShouldFireOnce_WhenTheTimeFallsInARepeatedHour()
    {
        // Arrange
        CronSchedule.TryCreate("30 2 * * *", out var schedule).ShouldBeTrue();
        var first = schedule.Next(At("2026-10-24T12:00:00Z"), Rome);

        // Act
        var second = schedule.Next(first, Rome);

        // Assert
        first.ShouldBe(At("2026-10-25T02:30:00+02:00"));
        second.ShouldBe(At("2026-10-26T02:30:00+01:00"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("* * * *")]
    [InlineData("* * * * * * *")]
    [InlineData("60 * * * *")]
    [InlineData("* 24 * * *")]
    [InlineData("* * 0 * *")]
    [InlineData("* * * 13 *")]
    [InlineData("* * * * 8")]
    [InlineData("5-1 * * * *")]
    [InlineData("*/0 * * * *")]
    [InlineData("a * * * *")]
    [InlineData("1,,2 * * * *")]
    [InlineData("0 0 30 2 *")]
    public void TryCreate_ShouldRefuse_WhenTheExpressionIsInvalid(string cron)
    {
        // Act
        var created = CronSchedule.TryCreate(cron, out _);

        // Assert
        created.ShouldBeFalse();
    }

    private static DateTimeOffset At(string text) =>
        DateTimeOffset.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
}
