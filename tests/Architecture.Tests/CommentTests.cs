using Xunit;

namespace Architecture.Tests;

/// <summary>
///     A code comment exists only to flag a hack or a strange behavior, and says so with a HACK: marker
///     (ADR-0023). XML docs and the Arrange, Act and Assert comments of a test (ADR-0025) are exempt;
///     generated migrations are left as the tooling writes them (ADR-0017).
/// </summary>
public sealed class CommentTests
{
    private const string Marker = "// HACK:";

    [Fact]
    public void Comment_ShouldStartWithHackMarker_WhenItAppearsInSource()
    {
        // Arrange
        var files = Solution.HandWrittenSourceFiles().Select(file => (File: file, Relative: Solution.Relative(file)));

        // Act
        var offenders = files.SelectMany(entry =>
                UnmarkedComments(File.ReadAllLines(entry.File)).Select(number => $"{entry.Relative}:{number}"))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("// Describes what the next line does")]
    [InlineData("var x = 1; // one")]
    [InlineData("/* block */")]
    [InlineData("// HACK without the colon")]
    public void UnmarkedComments_ShouldFlagALine_WhenItCarriesAnOrdinaryComment(string line)
    {
        // Act
        var flagged = UnmarkedComments([line]);

        // Assert
        flagged.ShouldBe([1]);
    }

    [Theory]
    [InlineData("// HACK: the driver reports seconds as a string")]
    [InlineData("var x = 1; // HACK: off by one in the driver")]
    [InlineData("// Arrange")]
    [InlineData("// Act")]
    [InlineData("// Assert")]
    [InlineData("/// <summary>Docs.</summary>")]
    [InlineData("var url = \"https://example.com\";")]
    [InlineData("var text = \"a // b\";")]
    [InlineData(""""    [InlineData("""<Compile Include="../a/**/*.cs" />""")]"""")]
    public void UnmarkedComments_ShouldSkipALine_WhenItIsMarkedExemptOrNotAComment(string line)
    {
        // Act
        var flagged = UnmarkedComments([line]);

        // Assert
        flagged.ShouldBeEmpty();
    }

    [Fact]
    public void UnmarkedComments_ShouldSkipContinuationLines_WhenTheBlockOpensWithHackMarker()
    {
        // Arrange
        var lines = new[] { "    // HACK: the driver reports seconds", "    // as a string.", "    var x = 1;" };

        // Act
        var flagged = UnmarkedComments(lines);

        // Assert
        flagged.ShouldBeEmpty();
    }

    private static List<int> UnmarkedComments(IReadOnlyList<string> lines)
    {
        var flagged = new List<int>();
        var inMarkedBlock = false;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var start = CommentStart(line);
            var isFullLine = start >= 0 && line[..start].Trim().Length == 0;
            var text = start < 0 ? string.Empty : line[start..];
            var isExempt = text.StartsWith("///", StringComparison.Ordinal) ||
                           text is "// Arrange" or "// Act" or "// Assert";
            var isMarked = text.StartsWith(Marker, StringComparison.Ordinal);
            var isContinuation = isFullLine && inMarkedBlock;
            if (start >= 0 && !isExempt && !isMarked && !isContinuation)
            {
                flagged.Add(i + 1);
            }

            inMarkedBlock = isFullLine && (isMarked || inMarkedBlock) && !isExempt;
        }

        return flagged;
    }

    private static int CommentStart(string line)
    {
        var inString = false;
        var i = 0;

        while (i < line.Length - 1)
        {
            var step = 1;
            if (!inString && string.CompareOrdinal(line, i, "\"\"\"", 0, 3) == 0)
            {
                var close = line.IndexOf("\"\"\"", i + 3, StringComparison.Ordinal);
                step = close < 0 ? line.Length : close + 3 - i;
            }
            else if (inString)
            {
                step = line[i] == '\\' ? 2 : 1;
                inString = line[i] != '"';
            }
            else if (line[i] == '"')
            {
                inString = true;
            }
            else if (line[i] == '/' && line[i + 1] is '/' or '*')
            {
                return i;
            }

            i += step;
        }

        return -1;
    }
}
