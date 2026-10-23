using System.Text.RegularExpressions;
using Xunit;

namespace Architecture.Tests;

/// <summary>
///     Every test follows ADR-0025, and no test project reaches for another mocking or assertion library.
/// </summary>
public sealed partial class TestStandardsTests
{
    private static readonly string[] BuildOutput = ["bin", "obj"];

    [Fact]
    public void Violations_ShouldBeEmpty_WhenEveryTestFollowsTheStandard()
    {
        // Arrange
        var tests = Path.Combine(Solution.Root(), "tests");
        var sources = from file in Directory.EnumerateFiles(tests, "*.cs", SearchOption.AllDirectories)
            let relative = Path.GetRelativePath(tests, file).Replace('\\', '/')
            where !relative.Split('/').Any(BuildOutput.Contains)
            select (relative, text: File.ReadAllText(file));

        // Act
        var breaches = sources.SelectMany(s => TestStandards.Violations(s.text).Select(v => $"{s.relative}: {v}"))
            .ToList();

        // Assert
        breaches.ShouldBeEmpty();
    }

    [Fact]
    public void Projects_ShouldNotReferenceAnotherMockingOrAssertionLibrary()
    {
        // Arrange
        var tests = Path.Combine(Solution.Root(), "tests");
        var projects = Directory.EnumerateFiles(tests, "*.csproj", SearchOption.AllDirectories);

        // Act
        var offenders = projects.Where(p => ForeignLibrary().IsMatch(File.ReadAllText(p))).ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Create_ShouldTrimTheName", "// Act\n// Assert\n")]
    [InlineData("Get_ShouldReturn404_WhenMissing", "// Arrange\n// Act\n// Assert\nAssert.Fail(\"x\");")]
    public void Violations_ShouldBeEmpty_WhenTheTestFollowsTheStandard(string name, string body)
    {
        // Arrange
        var source = Sample(name, body);

        // Act
        var breaches = TestStandards.Violations(source);

        // Assert
        breaches.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("A_name_in_prose", "// Act\n// Assert\n", "not named")]
    [InlineData("Create_ShouldTrim_WhenPadded", "// Assert\n", "missing // Act")]
    [InlineData("Create_ShouldTrim_WhenPadded", "// Act\n", "missing // Assert")]
    [InlineData("Create_ShouldTrim_WhenPadded", "// Act\n// Assert\nAssert.Equal(1, 1);", "Shouldly")]
    public void Violations_ShouldReportTheBreach_WhenTheTestBreaksTheStandard(string name, string body, string expected)
    {
        // Arrange
        var source = Sample(name, body);

        // Act
        var breaches = TestStandards.Violations(source);

        // Assert
        breaches.ShouldHaveSingleItem().ShouldContain(expected);
    }

    private static string Sample(string name, string body) => $"[Fact] public void {name}() {{\n{body}}}";

    [GeneratedRegex(@"Include=""(Moq|FakeItEasy|FluentAssertions|NFluent)""")]
    private static partial Regex ForeignLibrary();
}
