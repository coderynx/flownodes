using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Architecture.Tests;

/// <summary>
///     Modules never reference each other; each reaches only its own layers and Core's matching layer, and the host
///     reaches a module only through its Bootstrap (ADR-0018).
/// </summary>
public sealed partial class ModuleBoundaryTests
{
    private static readonly Dictionary<string, string[]> InnerLayers = new()
    {
        ["Contracts"] = [],
        ["Domain"] = [],
        ["Application"] = ["Domain"],
        ["Infrastructure"] = ["Application"],
        ["Presentation"] = ["Application"]
    };

    [Fact]
    public void ModuleProject_ShouldBeNamedAfterItsModuleAndLayer_WhenUnderPackages()
    {
        // Arrange
        var projects = Solution.ModuleProjects();

        // Act
        var offenders = projects.Where(project =>
                !Solution.Layers.Contains(project.Layer) ||
                !string.Equals(project.Name, $"{project.Module}.{project.Layer}", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(project.Directory) != project.Name)
            .Select(project => Solution.Relative(project.Path))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void ModuleProject_ShouldReferenceOnlyItsInnerLayersAndCore_WhenUnderPackages()
    {
        // Arrange
        var projects = Solution.ModuleProjects();

        // Act
        var offenders = projects.SelectMany(project => Solution.ProjectReferences(project.Path)
                .Select(ModuleProject.From)
                .Where(target => !MayReference(project, target))
                .Select(target => $"{project.Name} references {target.Name}"))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void CoreContracts_ShouldReferenceNothing_WhenUnderPackages()
    {
        // Arrange
        var project = Path.Combine(Solution.Root(), "packages/core/Core.Contracts/Core.Contracts.csproj");

        // Act
        var references = XDocument.Load(project)
            .Descendants()
            .Where(element =>
                element.Name.LocalName is "ProjectReference" or "PackageReference" or "FrameworkReference")
            .Select(element => (string?)element.Attribute("Include"))
            .ToList();

        // Assert
        references.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("engine", "Application", "engine", "Domain")]
    [InlineData("engine", "Infrastructure", "engine", "Application")]
    [InlineData("engine", "Presentation", "core", "Presentation")]
    [InlineData("core", "Presentation", "core", "Application")]
    [InlineData("registry", "Application", "core", "Contracts")]
    [InlineData("registry", "Infrastructure", "core", "Contracts")]
    public void MayReference_ShouldAllow_WhenTheTargetIsAnInnerLayerOrCore(
        string module,
        string layer,
        string targetModule,
        string targetLayer)
    {
        // Act
        var allowed = MayReference(Project(module, layer), Project(targetModule, targetLayer));

        // Assert
        allowed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("engine", "Application", "registry", "Application")]
    [InlineData("engine", "Domain", "engine", "Application")]
    [InlineData("engine", "Presentation", "engine", "Infrastructure")]
    [InlineData("engine", "Presentation", "core", "Infrastructure")]
    [InlineData("core", "Application", "engine", "Application")]
    [InlineData("registry", "Domain", "core", "Contracts")]
    [InlineData("registry", "Presentation", "core", "Contracts")]
    [InlineData("core", "Application", "core", "Contracts")]
    [InlineData("core", "Contracts", "core", "Domain")]
    public void MayReference_ShouldRefuse_WhenTheTargetCrossesAModuleOrPointsOutward(
        string module,
        string layer,
        string targetModule,
        string targetLayer)
    {
        // Act
        var allowed = MayReference(Project(module, layer), Project(targetModule, targetLayer));

        // Assert
        allowed.ShouldBeFalse();
    }

    [Fact]
    public void Host_ShouldReachModulesOnlyThroughBootstrap_WhenUnderApps()
    {
        // Arrange
        var files = Solution.SourceFiles("apps");

        // Act
        var offenders = files.SelectMany(file => File.ReadLines(file)
                .Select((line, index) => (Line: line, Number: index + 1))
                .Where(entry => BeyondBootstrap().IsMatch(entry.Line))
                .Select(entry => $"{Solution.Relative(file)}:{entry.Number}"))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void TestProject_ShouldReferenceOnlyItsOwnModuleAndCore_WhenItTestsOneModule()
    {
        // Arrange
        var projects = Directory.EnumerateFiles(
                Path.Combine(Solution.Root(), "tests"),
                "*.csproj",
                SearchOption.AllDirectories)
            .Where(project => Path.GetFileNameWithoutExtension(project) is not "Architecture.Tests");

        // Act
        var offenders = projects.SelectMany(project =>
            {
                var module = Path.GetFileNameWithoutExtension(project).Split('.')[0];

                return Solution.ProjectReferences(project)
                    .Select(ModuleProject.From)
                    .Where(target => !target.Module.Equals(module, StringComparison.OrdinalIgnoreCase) &&
                                     target.Module != Solution.Core)
                    .Select(target => $"{Path.GetFileName(project)} references {target.Name}");
            })
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    private static bool MayReference(ModuleProject project, ModuleProject target)
    {
        var sameModule = project.Module == target.Module;
        var innerLayer = sameModule && InnerLayers[project.Layer].Contains(target.Layer);
        var coreCounterpart = project.Module != Solution.Core && target.Module == Solution.Core &&
                              target.Layer == project.Layer;

        var contracts = project.Module != Solution.Core && target is { Module: Solution.Core, Layer: "Contracts" } &&
                        project.Layer is "Application" or "Infrastructure";

        return innerLayer || coreCounterpart || contracts;
    }

    private static ModuleProject Project(string module, string layer) =>
        new($"packages/{module}/{module}.{layer}/{module}.{layer}.csproj", $"{module}.{layer}", module, layer);

    [GeneratedRegex(@"\b\w+\.(Domain|Application|Infrastructure|Presentation)\.(?!Bootstrap\b)")]
    private static partial Regex BeyondBootstrap();
}
