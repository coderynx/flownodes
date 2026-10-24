using System.Xml.Linq;
using Xunit;

namespace Architecture.Tests;

/// <summary>
///     A module's tests split into unit tests, which run its Domain and handlers in memory with everything else
///     substituted, and integration tests, which run it in a real host against Postgres (ADR-0025).
/// </summary>
public sealed class TestProjectTests
{
    private static readonly string[] UnitTestLayers = ["Contracts", "Domain", "Application"];
    private static readonly string[] HostPackages = ["Microsoft.AspNetCore.TestHost", "Testcontainers", "WolverineFx"];
    private static readonly string[] MockingPackages = ["NSubstitute", "MockQueryable"];

    [Fact]
    public void TestProject_ShouldBeAModulesUnitOrIntegrationTests_WhenItIsNotTheArchitectureTests()
    {
        // Arrange
        var projects = TestProjects().Where(project => Name(project) is not "Architecture.Tests");

        // Act
        var offenders = projects.Where(project =>
            {
                var name = Name(project);
                var module = name.Split('.')[0];
                var directory = Solution.Relative(Path.GetDirectoryName(project)!);
                var isNamedForItsKind = name == $"{module}.UnitTests" || name == $"{module}.IntegrationTests";
                return !isNamedForItsKind || directory != $"tests/{module.ToLowerInvariant()}/{name}";
            })
            .Select(Solution.Relative)
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void UnitTestProject_ShouldReferenceOnlyContractsDomainAndApplication_WhenItTestsAModule()
    {
        // Arrange
        var projects = TestProjects().Where(project => Name(project).EndsWith(".UnitTests", StringComparison.Ordinal));

        // Act
        var offenders = projects.SelectMany(project => Solution.ProjectReferences(project)
                .Select(ModuleProject.From)
                .Where(target => !UnitTestLayers.Contains(target.Layer))
                .Select(target => $"{Name(project)} references {target.Name}")
                .Concat(
                    Packages(project)
                        .Where(package => HostPackages.Any(host => package.StartsWith(host, StringComparison.Ordinal)))
                        .Select(package => $"{Name(project)} references {package}")))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void TestProject_ShouldNotReferenceAMockingLibrary_WhenItIsNotUnitTests()
    {
        // Arrange
        var projects = TestProjects().Where(project => !Name(project).EndsWith(".UnitTests", StringComparison.Ordinal));

        // Act
        var offenders = projects.SelectMany(project => Packages(project)
                .Where(package => MockingPackages.Any(mock => package.StartsWith(mock, StringComparison.Ordinal)))
                .Select(package => $"{Name(project)} references {package}"))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    private static IEnumerable<string> TestProjects() =>
        Directory.EnumerateFiles(Path.Combine(Solution.Root(), "tests"), "*.csproj", SearchOption.AllDirectories)
            .Where(project => !Solution.Relative(project).Split('/').Any(part => part is "bin" or "obj"));

    private static string Name(string project) => Path.GetFileNameWithoutExtension(project);

    private static IEnumerable<string> Packages(string project) =>
        XDocument.Load(project)
            .Descendants("PackageReference")
            .Select(reference => (string?)reference.Attribute("Include") ?? string.Empty);
}
