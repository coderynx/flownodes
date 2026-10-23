using System.Text.RegularExpressions;
using Xunit;

namespace Architecture.Tests;

/// <summary>
///     An analyzer rule is switched off in .editorconfig with its reason, never inline (ADR-0016), so every exception
///     to the rules is in one file a reviewer can read. Test access is granted in the .csproj, never in source
///     (ADR-0017).
/// </summary>
public sealed partial class SuppressionTests
{
    [Fact]
    public void InlineSuppression_ShouldNotAppear_WhenSourceIsHandWritten()
    {
        // Arrange
        var files = Solution.HandWrittenSourceFiles();

        // Act
        var offenders = files.Where(file => InlineSuppression().IsMatch(File.ReadAllText(file)))
            .Select(Solution.Relative)
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void InternalsVisibleTo_ShouldNotAppearAsAnAttribute_WhenSourceIsHandWritten()
    {
        // Arrange
        var files = Solution.HandWrittenSourceFiles();

        // Act
        var offenders = files.Where(file => InternalsVisibleToAttribute().IsMatch(File.ReadAllText(file)))
            .Select(Solution.Relative)
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("#pragma warning disable S1144")]
    [InlineData("#pragma warning disable CS0168, IDE0051")]
    [InlineData("""[SuppressMessage("Sonar Code Smell", "S1144")]""")]
    [InlineData("""[assembly: SuppressMessage("Major Code Smell", "S108")]""")]
    [InlineData("""    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060")]""")]
    public void InlineSuppression_ShouldMatch_WhenSourceSuppressesARule(string source)
    {
        // Act
        var matches = InlineSuppression().IsMatch(source);

        // Assert
        matches.ShouldBeTrue();
    }

    [Theory]
    [InlineData("""    [InlineData("#pragma warning disable S1144")]""")]
    [InlineData("#pragma warning restore S1144")]
    public void InlineSuppression_ShouldNotMatch_WhenSourceDoesNotSuppressARule(string source)
    {
        // Act
        var matches = InlineSuppression().IsMatch(source);

        // Assert
        matches.ShouldBeFalse();
    }

    [Theory]
    [InlineData("[assembly: InternalsVisibleTo(\"Registry.Tests\")]")]
    [InlineData("[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"Registry.Tests\")]")]
    public void InternalsVisibleToAttribute_ShouldMatch_WhenSourceGrantsAccess(string source)
    {
        // Act
        var matches = InternalsVisibleToAttribute().IsMatch(source);

        // Assert
        matches.ShouldBeTrue();
    }

    [GeneratedRegex(
        """^\s*(\#pragma\s+warning\s+disable\b|\[(assembly:\s*)?[\w.]*SuppressMessage\b)""",
        RegexOptions.Multiline)]
    private static partial Regex InlineSuppression();

    [GeneratedRegex(@"^\s*\[assembly:\s*[\w.]*InternalsVisibleTo\b", RegexOptions.Multiline)]
    private static partial Regex InternalsVisibleToAttribute();
}
