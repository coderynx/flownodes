using System.Globalization;

namespace Engine.Domain;

internal static class Durations
{
    public static TimeSpan Parse(string text)
    {
        var digits = text.TakeWhile(char.IsDigit).Count();
        var amount = double.Parse(text[..digits], CultureInfo.InvariantCulture);

        return text[digits..] switch
        {
            "ms" => TimeSpan.FromMilliseconds(amount),
            "s" => TimeSpan.FromSeconds(amount),
            "m" => TimeSpan.FromMinutes(amount),
            _ => TimeSpan.FromHours(amount)
        };
    }
}
