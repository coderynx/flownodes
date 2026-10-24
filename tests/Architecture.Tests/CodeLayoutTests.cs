using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Architecture.Tests;

/// <summary>
///     Checks the parts of the layout standard (ADR-0022) that no script can apply: C# lines stay within 120 columns
///     unless their overlength is a string, raw string or URL that cannot be broken without changing its meaning, and
///     an <c>if</c> joining three or more predicates names them in a Boolean local first.
/// /// </summary>
public sealed class CodeLayoutTests
{
    private const int MaxColumns = 120;
    private const int LongStringLength = 60;

    [Fact]
    public void IfStatement_ShouldHaveAtMostTwoPredicates_WhenCSharpSource()
    {
        // Arrange
        var files = Solution.HandWrittenSourceFiles();

        // Act
        var offenders = files.SelectMany(file =>
                OvercomplexConditions(File.ReadAllText(file)).Select(number => $"{Solution.Relative(file)}:{number}"))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void OvercomplexConditions_ShouldFlagAnIf_WhenItJoinsThreePredicates()
    {
        // Arrange
        var source = "class C { void M(bool a, bool b, bool c) { if (a && (b || !c)) { } } }";

        // Act
        var flagged = OvercomplexConditions(source);

        // Assert
        flagged.ShouldBe([1]);
    }

    [Fact]
    public void OvercomplexConditions_ShouldSkipAnIf_WhenExtraPredicatesSitInsideALambda()
    {
        // Arrange
        var source = "class C { void M(bool a, int[] xs) { if (a && xs.Any(x => x > 0 && x < 9)) { } } }";

        // Act
        var flagged = OvercomplexConditions(source);

        // Assert
        flagged.ShouldBeEmpty();
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

    private static List<int> OvercomplexConditions(string source) =>
        CSharpSyntaxTree.ParseText(source)
            .GetRoot()
            .DescendantNodes()
            .OfType<IfStatementSyntax>()
            .Where(statement => LogicalOperators(statement.Condition) >= 2)
            .Select(statement => statement.GetLocation().GetLineSpan().StartLinePosition.Line + 1)
            .ToList();

    private static int LogicalOperators(ExpressionSyntax expression) => expression switch
    {
        BinaryExpressionSyntax binary when IsLogical(binary) => 1 + LogicalOperators(binary.Left) +
                                                                LogicalOperators(binary.Right),
        ParenthesizedExpressionSyntax parenthesized => LogicalOperators(parenthesized.Expression),
        PrefixUnaryExpressionSyntax unary => LogicalOperators(unary.Operand),
        _ => 0
    };

    private static bool IsLogical(BinaryExpressionSyntax binary) =>
        binary.Kind() is SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression;

    private static List<int> OverlongLines(IReadOnlyList<string> lines)
    {
        var flagged = new List<int>();
        var inRawString = false;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var startedInRawString = inRawString;
            var limitInsideString = LimitFallsInsideString(line, ref inRawString);
            var isWrappableOverlong = line.Length > MaxColumns && !startedInRawString && !limitInsideString &&
                                      !LimitFallsInsideUrl(line);
            if (isWrappableOverlong)
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
