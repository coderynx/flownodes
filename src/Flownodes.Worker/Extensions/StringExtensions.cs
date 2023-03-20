namespace Flownodes.Worker.Extensions;

public static class StringExtensions
{
    public static int? ToNullableInt(this string? s)
    {
        if (int.TryParse(s, out var i)) return i;
        return null;
    }
}