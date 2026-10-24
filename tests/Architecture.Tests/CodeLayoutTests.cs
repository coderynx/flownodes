using Xunit;

namespace Architecture.Tests;

/// <summary>
///     C# lines stay within 120 columns and blank lines do not repeat (ADR-0022). A line whose overlength is a string,
///     raw string or URL that cannot be broken without changing its meaning is exempt; generated migrations keep their
///     generated layout apart from namespaces, imports and empty blank lines (ADR-0022).
/// </summary>
public sealed class CodeLayoutTests
{
    private const int MaxColumns = 120;
    private const int LongStringLength = 60;

    [Fact]
    public void SourceFile_ShouldMatchProjectAndDirectoryNamespace_WhenCSharpSource()
    {
        // Arrange
        var root = Solution.Root();
        var files = new[] { "apps", "packages", "tests" }
            .SelectMany(directory => Directory.EnumerateFiles(
                Path.Combine(root, directory),
                "*.csproj",
                SearchOption.AllDirectories))
            .SelectMany(project =>
                Directory.EnumerateFiles(Path.GetDirectoryName(project)!, "*.cs", SearchOption.AllDirectories)
                    .Where(file => !Path.GetRelativePath(Path.GetDirectoryName(project)!, file)
                        .Split(Path.DirectorySeparatorChar)
                        .Any(part => part is "bin" or "obj" or "Generated"))
                    .Select(file => (Project: project, File: file)));

        // Act
        var offenders = new List<string>();

        foreach (var entry in files)
        {
            var projectDirectory = Path.GetDirectoryName(entry.Project)!;
            var projectName = Path.GetFileNameWithoutExtension(entry.Project);
            var suffix = Path.GetRelativePath(projectDirectory, Path.GetDirectoryName(entry.File)!)
                .Replace(Path.DirectorySeparatorChar, '.');
            var expected = suffix == "." ? projectName : $"{projectName}.{suffix}";
            var actual = File.ReadLines(entry.File)
                .FirstOrDefault(line => line.StartsWith("namespace ", StringComparison.Ordinal))
                ?.Replace("namespace ", string.Empty, StringComparison.Ordinal)
                .TrimEnd('{', ';', ' ');
            if (actual != expected)
            {
                offenders.Add(Solution.Relative(entry.File));
            }
        }

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void SourceFile_ShouldOrderUsingDirectives_WhenCSharpSource()
    {
        // Arrange
        var files = Solution.SourceFiles("apps", "packages", "tests");

        // Act
        var offenders = files.Where(file =>
            {
                var usings = File.ReadLines(file)
                    .TakeWhile(line => !line.StartsWith("namespace ", StringComparison.Ordinal))
                    .Where(line => line.StartsWith("using ", StringComparison.Ordinal) && line.EndsWith(';'))
                    .Select(line => line[6..^1])
                    .ToList();
                var ordered = usings.OrderBy(line => line.Contains(" = ", StringComparison.Ordinal))
                    .ThenBy(line => line is "System" || line.StartsWith("System.", StringComparison.Ordinal) ? 0 : 1)
                    .ThenBy(line => line, StringComparer.Ordinal);
                return !usings.SequenceEqual(ordered);
            })
            .Select(file => Solution.Relative(file))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void SourceLine_ShouldHaveEmptyBlankLines_WhenCSharpSource()
    {
        // Arrange
        var files = Solution.SourceFiles("apps", "packages", "tests");

        // Act
        var offenders = files.SelectMany(file => File.ReadLines(file)
                .Select((line, index) => (Line: line, Number: index + 1))
                .Where(entry => entry.Line.Length > 0 && string.IsNullOrWhiteSpace(entry.Line))
                .Select(entry => $"{Solution.Relative(file)}:{entry.Number}"))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void SourceLine_ShouldNotHaveConsecutiveBlankLines_WhenCSharpSource()
    {
        // Arrange
        var files = Solution.HandWrittenSourceFiles().Select(file => (File: file, Relative: Solution.Relative(file)));

        // Act
        var offenders = new List<string>();

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file.File);

            for (var index = 1; index < lines.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(lines[index]) && string.IsNullOrWhiteSpace(lines[index - 1]))
                {
                    offenders.Add($"{file.Relative}:{index + 1}");
                }
            }
        }

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void SourceLine_ShouldNotExceed120Columns_WhenItCanBeWrapped()
    {
        // Arrange
        var files = Solution.HandWrittenSourceFiles().Select(file => (File: file, Relative: Solution.Relative(file)));

        // Act
        var offenders = files.SelectMany(entry =>
                OverlongLines(File.ReadAllLines(entry.File)).Select(number => $"{entry.Relative}:{number}"))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void OverlongLines_ShouldFlagALine_WhenItPassesTheLimitOutsideAString()
    {
        // Arrange
        var line = "var total = " + string.Join(" + ", Enumerable.Repeat("firstValue", 12)) + ";";

        // Act
        var flagged = OverlongLines([line]);

        // Assert
        flagged.ShouldBe([1]);
    }

    [Fact]
    public void OverlongLines_ShouldSkipALine_WhenTheLimitFallsInsideAStringLiteral()
    {
        // Arrange
        var line = "var text = \"" + new string('x', 130) + "\";";

        // Act
        var flagged = OverlongLines([line]);

        // Assert
        flagged.ShouldBeEmpty();
    }

    [Fact]
    public void OverlongLines_ShouldSkipALine_WhenOnlyClosingPunctuationPassesTheLimit()
    {
        // Arrange
        var line = "    \"" + new string('x', 114) + "\"),";

        // Act
        var flagged = OverlongLines([line]);

        // Assert
        flagged.ShouldBeEmpty();
    }

    [Fact]
    public void OverlongLines_ShouldSkipALine_WhenItSitsInsideARawString()
    {
        // Arrange
        var lines = new[] { "var text = \"\"\"", new string('x', 130), "\"\"\";" };

        // Act
        var flagged = OverlongLines(lines);

        // Assert
        flagged.ShouldBeEmpty();
    }

    [Fact]
    public void OverlongLines_ShouldSkipALine_WhenTheLimitFallsInsideAUrl()
    {
        // Arrange
        var line = "/// <see href=\"https://example.com/" + new string('a', 100) + "\">docs</see>";
        var comment = "// " + new string('w', 100) + " https://example.com/" + new string('a', 30);

        // Act
        var flagged = OverlongLines([line, comment]);

        // Assert
        flagged.ShouldBeEmpty();
    }

    private static List<int> OverlongLines(IReadOnlyList<string> lines)
    {
        var flagged = new List<int>();
        var inRawString = false;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var startedInRawString = inRawString;
            var limitInsideString = LimitFallsInsideString(line, ref inRawString);
            if (line.Length > MaxColumns && !startedInRawString && !limitInsideString && !LimitFallsInsideUrl(line))
            {
                flagged.Add(i + 1);
            }
        }

        return flagged;
    }

    private static bool LimitFallsInsideString(string line, ref bool inRawString)
    {
        var codeEnd = line.TrimEnd(',', ';', ')', ']').Length;
        var inString = false;
        var inside = false;
        var stringStart = 0;
        var i = 0;

        while (i < line.Length)
        {
            if (i == MaxColumns)
            {
                inside |= inRawString || inString;
            }

            var step = 1;
            var tripleQuote = string.CompareOrdinal(line, i, "\"\"\"", 0, 3) == 0;
            var closes = false;
            if (inRawString)
            {
                closes = tripleQuote;
                step = closes ? 3 : 1;
                inRawString &= !closes;
            }
            else if (inString)
            {
                step = line[i] == '\\' ? 2 : 1;
                closes = line[i] == '"';
                inString &= !closes;
            }
            else if (tripleQuote || line[i] == '"')
            {
                stringStart = i;
                step = tripleQuote ? 3 : 1;
                inRawString = tripleQuote;
                inString = !tripleQuote;
            }

            i += step;
            inside |= closes && i >= codeEnd && i - stringStart >= LongStringLength;
        }

        return inside;
    }

    private static bool LimitFallsInsideUrl(string line)
    {
        if (line[MaxColumns] == ' ')
        {
            return false;
        }

        var start = line.LastIndexOf(' ', MaxColumns) + 1;
        var end = line.IndexOf(' ', MaxColumns);
        var token = end < 0 ? line[start..] : line[start..end];
        return token.Contains("://", StringComparison.Ordinal);
    }
}
