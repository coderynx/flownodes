using System.Text.RegularExpressions;

namespace Architecture.Tests;

/// <summary>
///     Reads test source the way ADR-0025 asks it to be written: names, the Act and Assert markers,
///     Shouldly rather than <c>Assert.*</c>. Text-based, so it needs no compiler to inspect a file.
/// </summary>
internal static partial class TestStandards
{
    /// <summary>What is wrong with each test method in <paramref name="source" />, one line per breach.</summary>
    /// <param name="source">The text of one C# test file.</param>
    /// <returns>An empty list when every test follows the standard.</returns>
    public static IReadOnlyList<string> Violations(string source)
    {
        var breaches = new List<string>();

        foreach (Match method in TestMethod().Matches(source))
        {
            var name = method.Groups["name"].Value;
            var body = Body(source, method.Index + method.Length);

            if (!Name().IsMatch(name))
            {
                breaches.Add($"{name}: not named Member_ShouldOutcome_WhenCondition");
            }

            if (!body.Contains("// Act", StringComparison.Ordinal))
            {
                breaches.Add($"{name}: missing // Act");
            }

            if (!body.Contains("// Assert", StringComparison.Ordinal))
            {
                breaches.Add($"{name}: missing // Assert");
            }

            if (XunitAssertion().IsMatch(body))
            {
                breaches.Add($"{name}: uses Assert.* instead of Shouldly");
            }
        }

        return breaches;
    }

    private static string Body(string source, int from)
    {
        var open = source.IndexOf('{', from);
        var depth = 0;

        for (var i = open; i < source.Length; i++)
        {
            depth += source[i] switch { '{' => 1, '}' => -1, _ => 0 };
            if (depth == 0)
            {
                return source[open..(i + 1)];
            }
        }

        return source[open..];
    }

    [GeneratedRegex(
        @"\[(Fact|Theory)\b[^\]]*\](\s*\[[^\]]*\])*\s*public\s+(async\s+)?(Task|ValueTask|void)\s+(?<name>\w+)\s*\(([^)]*)\)")]
    private static partial Regex TestMethod();

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9]*_Should[A-Z][A-Za-z0-9]*(_When[A-Z][A-Za-z0-9]*)?$")]
    private static partial Regex Name();

    [GeneratedRegex(@"\bAssert\.(?!Fail\b|Skip\w*\b)\w+")]
    private static partial Regex XunitAssertion();
}
