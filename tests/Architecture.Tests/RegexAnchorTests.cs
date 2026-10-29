using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Architecture.Tests;

/// <summary>
///     Checks that no regex pattern under <c>packages/</c> ends in <c>$</c>, which also matches before a trailing
///     newline: a pattern ends in <c>\z</c> instead.
/// </summary>
public sealed class RegexAnchorTests
{
    [Fact]
    public void RegexPattern_ShouldNotEndInDollar_WhenUnderPackages()
    {
        // Arrange
        var files = Solution.SourceFiles("packages");

        // Act
        var offenders = files.SelectMany(file =>
                DollarTerminatedPatterns(File.ReadAllText(file))
                    .Select(number => $"{Solution.Relative(file)}:{number}"))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void DollarTerminatedPatterns_ShouldFlagAPattern_WhenItEndsInDollar()
    {
        // Arrange
        var source = """
                     partial class C
                     {
                         [GeneratedRegex("^[a-z]+$")]
                         static partial Regex A();

                         static readonly Regex B = new(@"^\d+$");
                         static readonly Regex D = new Regex("^x$");
                     }
                     """;

        // Act
        var flagged = DollarTerminatedPatterns(source);

        // Assert
        flagged.ShouldBe([3, 6, 7]);
    }

    [Fact]
    public void DollarTerminatedPatterns_ShouldSkipAPattern_WhenItEndsInBackslashZ()
    {
        // Arrange
        var source = """
                     partial class C
                     {
                         [GeneratedRegex(@"^[a-z]+\z")]
                         static partial Regex A();
                     }
                     """;

        // Act
        var flagged = DollarTerminatedPatterns(source);

        // Assert
        flagged.ShouldBeEmpty();
    }

    [Fact]
    public void DollarTerminatedPatterns_ShouldSkipAPattern_WhenDollarIsInTheMiddleOrInACharacterClass()
    {
        // Arrange
        var source = """
                     partial class C
                     {
                         [GeneratedRegex(@"^a$|^b\z")]
                         static partial Regex A();

                         [GeneratedRegex(@"^[a$]+\z")]
                         static partial Regex B();

                         [GeneratedRegex(@"^[$]")]
                         static partial Regex D();

                         [GeneratedRegex(@"^cost \$")]
                         static partial Regex E();
                     }
                     """;

        // Act
        var flagged = DollarTerminatedPatterns(source);

        // Assert
        flagged.ShouldBeEmpty();
    }

    private static List<int> DollarTerminatedPatterns(string source) =>
        CSharpSyntaxTree.ParseText(source)
            .GetRoot()
            .DescendantNodes()
            .Select(PatternArgument)
            .OfType<LiteralExpressionSyntax>()
            .Where(literal => literal.IsKind(SyntaxKind.StringLiteralExpression))
            .Where(literal => EndsInDollar(literal.Token.ValueText))
            .Select(literal => literal.GetLocation().GetLineSpan().StartLinePosition.Line + 1)
            .ToList();

    private static ExpressionSyntax? PatternArgument(SyntaxNode node) => node switch
    {
        AttributeSyntax attribute when attribute.Name.ToString() is "GeneratedRegex" or "GeneratedRegexAttribute" =>
            attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression,
        BaseObjectCreationExpressionSyntax creation when IsRegex(creation) => creation.ArgumentList?.Arguments
            .FirstOrDefault()
            ?.Expression,
        _ => null
    };

    private static bool IsRegex(BaseObjectCreationExpressionSyntax creation) =>
        creation is ImplicitObjectCreationExpressionSyntax implicitCreation
            ? implicitCreation.Parent is EqualsValueClauseSyntax
            {
                Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration }
            } && declaration.Type.ToString() == "Regex"
            : ((ObjectCreationExpressionSyntax)creation).Type.ToString() is "Regex"
            or "System.Text.RegularExpressions.Regex";

    private static bool EndsInDollar(string pattern)
    {
        if (!pattern.EndsWith('$'))
        {
            return false;
        }

        var backslashes = pattern.Length - 1 - pattern[..^1].TrimEnd('\\').Length;
        return backslashes % 2 == 0;
    }
}
