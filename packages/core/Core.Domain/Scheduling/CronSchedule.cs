using System.Globalization;

namespace Core.Domain.Scheduling;

/// <summary>
/// A cron expression that a schedule trigger of a Flow fires on: five fields (minute, hour, day of month, month,
/// day of week) or six with seconds first. Each field takes <c>*</c>, a number, a range <c>a-b</c>, a step
/// <c>/n</c> and comma-separated lists. Day of week runs from 0 to 7, both Sunday. When day of month and day of week
/// are both restricted, a day matching either fires.
/// </summary>
public sealed class CronSchedule
{
    private const int SearchDays = 366 * 8;
    private static readonly (int Min, int Max)[] Bounds = [(0, 59), (0, 23), (1, 31), (1, 12), (0, 7)];
    private readonly int[] _seconds;
    private readonly int[] _minutes;
    private readonly int[] _hours;
    private readonly HashSet<int> _days;
    private readonly HashSet<int> _months;
    private readonly HashSet<int> _weekdays;
    private readonly bool _anyDay;
    private readonly bool _anyWeekday;

    private CronSchedule(
        int[] seconds,
        int[] minutes,
        int[] hours,
        HashSet<int> days,
        HashSet<int> months,
        HashSet<int> weekdays,
        bool anyDay,
        bool anyWeekday)
    {
        _seconds = seconds;
        _minutes = minutes;
        _hours = hours;
        _days = days;
        _months = months;
        _weekdays = weekdays;
        _anyDay = anyDay;
        _anyWeekday = anyWeekday;
    }

    /// <summary>
    /// Builds a schedule from an expression that has already been validated.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <returns>The schedule.</returns>
    /// <exception cref="ArgumentException">The expression is not a valid cron expression.</exception>
    public static CronSchedule Create(string expression) =>
        TryCreate(expression, out var schedule)
            ? schedule
            : throw new ArgumentException($"'{expression}' is not a cron expression.", nameof(expression));

    /// <summary>
    /// Parses a cron expression, refusing one that could never fire, such as the 30th of February.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="schedule">The schedule, when the expression is valid.</param>
    /// <returns><see langword="true"/> when the expression is valid.</returns>
    public static bool TryCreate(string expression, out CronSchedule schedule)
    {
        schedule = null!;
        var fields = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length is not (5 or 6))
        {
            return false;
        }

        var offset = fields.Length - 5;
        var parsed = new List<int>[5];
        for (var index = 0; index < 5; index++)
        {
            if (Field(fields[offset + index], Bounds[index].Min, Bounds[index].Max) is not { } values)
            {
                return false;
            }

            parsed[index] = values;
        }

        var seconds = offset == 1 ? Field(fields[0], 0, 59) : [0];
        if (seconds is null)
        {
            return false;
        }

        var (minutes, hours, days, months) = (parsed[0], parsed[1], parsed[2], parsed[3]);
        var anyDay = fields[offset + 2].StartsWith('*');
        var anyWeekday = fields[offset + 4].StartsWith('*');
        var reachable = !anyWeekday ||
                        months.Exists(month => days.Exists(day => day <= DateTime.DaysInMonth(2028, month)));
        if (!reachable)
        {
            return false;
        }

        schedule = new CronSchedule(
            [.. seconds.Order()],
            [.. minutes.Order()],
            [.. hours.Order()],
            [.. days],
            [.. months],
            [.. parsed[4].Select(day => day % 7)],
            anyDay,
            anyWeekday);

        return true;
    }

    /// <summary>
    /// Finds the first time strictly after a given instant at which the schedule fires in a time zone. A time that
    /// falls in a daylight-saving gap fires at the first valid time after it, and a time in a repeated hour fires once,
    /// at its first occurrence.
    /// </summary>
    /// <param name="after">The instant to search from.</param>
    /// <param name="zone">The time zone the expression is read in.</param>
    /// <returns>The next firing.</returns>
    public DateTimeOffset Next(DateTimeOffset after, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTime(after, zone).DateTime;
        var whole = local.Ticks - (local.Ticks % TimeSpan.TicksPerSecond);
        var start = new DateTime(whole, DateTimeKind.Unspecified).AddSeconds(1);
        for (var day = start.Date; day < start.Date.AddDays(SearchDays); day = day.AddDays(1))
        {
            if (!_months.Contains(day.Month) || !DayMatches(day))
            {
                continue;
            }

            foreach (var candidate in Times(day).Where(candidate => candidate >= start))
            {
                var instant = Resolve(candidate, zone);
                if (instant > after)
                {
                    return instant;
                }
            }
        }

        throw new InvalidOperationException("The schedule never fires.");
    }

    private IEnumerable<DateTime> Times(DateTime day) =>
        from hour in _hours
        from minute in _minutes
        from second in _seconds
        select day.AddHours(hour).AddMinutes(minute).AddSeconds(second);

    private bool DayMatches(DateTime day)
    {
        var dayMatches = _days.Contains(day.Day);
        var weekdayMatches = _weekdays.Contains((int)day.DayOfWeek);
        if (_anyDay || _anyWeekday)
        {
            return dayMatches && weekdayMatches;
        }

        return dayMatches || weekdayMatches;
    }

    private static DateTimeOffset Resolve(DateTime local, TimeZoneInfo zone)
    {
        while (zone.IsInvalidTime(local))
        {
            local = local.AddSeconds(-local.Second).AddMinutes(1);
        }

        return zone.IsAmbiguousTime(local)
            ? new DateTimeOffset(local, zone.GetAmbiguousTimeOffsets(local).Max())
            : new DateTimeOffset(local, zone.GetUtcOffset(local));
    }

    private static List<int>? Field(string text, int min, int max)
    {
        var values = new List<int>();
        foreach (var part in text.Split(','))
        {
            var range = Part(part, min, max);
            if (range is null)
            {
                return null;
            }

            values.AddRange(range);
        }

        return [.. values.Distinct()];
    }

    private static IEnumerable<int>? Part(string part, int min, int max)
    {
        var pieces = part.Split('/');
        var step = pieces.Length == 1 ? 1 : Step(pieces);
        if (step < 1)
        {
            return null;
        }

        int from;
        int to;
        var bounds = pieces[0].Split('-');
        if (pieces[0] == "*")
        {
            (from, to) = (min, max);
        }
        else if (bounds.Length == 1 && Number(bounds[0], out from))
        {
            to = pieces.Length == 2 ? max : from;
        }
        else if (bounds.Length != 2 || !Range(bounds, out from, out to))
        {
            return null;
        }

        var inBounds = from >= min && to <= max;

        return !inBounds || from > to
            ? null
            : Enumerable.Range(from, to - from + 1).Where(value => (value - from) % step == 0);
    }

    private static bool Range(string[] bounds, out int from, out int to)
    {
        to = 0;

        return Number(bounds[0], out from) && Number(bounds[1], out to);
    }

    private static int Step(string[] pieces) => pieces.Length == 2 && Number(pieces[1], out var step) ? step : 0;

    private static bool Number(string text, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
