using System.Security.Claims;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Bootstrap;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Architecture.Tests;

/// <summary>
///     An HTTP use case is one slice mirrored across its module's Application and Presentation layers (ADR-0020).
/// </summary>
public sealed class HttpUseCaseTests
{
    private static readonly string[] RequestFiles = ["Command.cs", "Query.cs"];
    private static readonly string[] RequestBases = ["IRequest", "ICommand", "IQuery"];
    private static readonly string[] HandlerBases = ["IRequestHandler", "ICommandHandler", "IQueryHandler"];
    private static readonly string[] BindingAttributes = ["FromRoute", "FromQuery"];

    [Fact]
    public void Request_ShouldLiveInACommandOrQueryFileOfItsUseCase_WhenDeclaredInApplication()
    {
        // Arrange
        var files = LayerSources("Application");

        // Act
        var offenders = files.Where(file => Declares(file.Tree, RequestBases.Concat(HandlerBases)))
            .Where(file => !RequestFiles.Contains(Path.GetFileName(file.Path)) || UseCasePath(file).Length == 0)
            .Select(file => Solution.Relative(file.Path))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Endpoint_ShouldMirrorAnApplicationUseCase_WhenDeclaredInPresentation()
    {
        // Arrange
        var files = LayerSources("Presentation");

        // Act
        var offenders = files.Where(file => Declares(file.Tree, ["IEndpoint"]))
            .Where(file =>
            {
                var useCase = UseCasePath(file);
                var application = Path.Combine(
                    Path.GetDirectoryName(file.Project.Directory)!,
                    file.Project.Name.Replace(".Presentation", ".Application", StringComparison.Ordinal),
                    useCase);
                var isEndpointFile = Path.GetFileName(file.Path) == "Endpoint.cs";

                return !isEndpointFile || useCase.Length == 0 || !Directory.Exists(application);
            })
            .Select(file => Solution.Relative(file.Path))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void RouteAndQueryValues_ShouldBindThroughARequestRecord_WhenDeclaredInPresentation()
    {
        // Arrange
        var files = LayerSources("Presentation");

        // Act
        var offenders = files.SelectMany(file => file.Tree.GetRoot()
                .DescendantNodes()
                .OfType<AttributeSyntax>()
                .Where(attribute => BindingAttributes.Contains(attribute.Name.ToString()))
                .Where(attribute => attribute.FirstAncestorOrSelf<RecordDeclarationSyntax>() is null)
                .Select(attribute => $"{Solution.Relative(file.Path)}:{Line(attribute)}"))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void ModulePresentation_ShouldNotWriteTheApiVersion_WhenMappingRoutes()
    {
        // Arrange
        var files = LayerSources("Presentation").Where(file => file.Project.Module != Solution.Core);

        // Act
        var offenders = files.Where(file => File.ReadAllText(file.Path).Contains("\"/api", StringComparison.Ordinal))
            .Select(file => Solution.Relative(file.Path))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Request_ShouldCarryNoHttpOrCallerTypes_WhenHandledInApplication()
    {
        // Arrange
        var types = Solution.Assemblies("Application").SelectMany(assembly => assembly.GetTypes());

        // Act
        var offenders = types.SelectMany(request => request.GetInterfaces()
                .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>))
                .SelectMany(contract => contract.GetGenericArguments())
                .Concat(request.GetProperties().Select(property => property.PropertyType))
                .SelectMany(Flatten)
                .Where(IsHttpOrCallerType)
                .Select(leak => $"{request.FullName} carries {leak.FullName}"))
            .Distinct()
            .ToList();

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Endpoint_ShouldCarryAnOperationName_WhenMapped()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        Core.Application.Bootstrap.DependencyInjection.AddCqrs(builder, [.. Solution.Assemblies("Application")]);
        Core.Presentation.Bootstrap.DependencyInjection.AddPresentation(builder);
        var app = builder.Build();
        app.MapEndpoints([.. Solution.Assemblies("Presentation")]);

        // Act
        var unnamed = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>() is null)
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToList();

        // Assert
        unnamed.ShouldBeEmpty();
    }

    private static IEnumerable<(string Path, ModuleProject Project, SyntaxTree Tree)> LayerSources(string layer) =>
        Solution.ModuleProjects()
            .Where(project => project.Layer == layer)
            .SelectMany(project => Solution.SourceFiles(Solution.Relative(project.Directory))
                .Select(file => (file, project, CSharpSyntaxTree.ParseText(File.ReadAllText(file)))));

    private static bool Declares(SyntaxTree tree, IEnumerable<string> bases)
    {
        var names = bases.ToHashSet();

        return tree.GetRoot()
            .DescendantNodes()
            .OfType<BaseTypeSyntax>()
            .Select(baseType => baseType.Type switch
            {
                GenericNameSyntax generic => generic.Identifier.Text,
                QualifiedNameSyntax { Right: GenericNameSyntax generic } => generic.Identifier.Text,
                var other => other.ToString()
            })
            .Any(names.Contains);
    }

    private static string UseCasePath((string Path, ModuleProject Project, SyntaxTree Tree) file)
    {
        var relative = Path.GetRelativePath(file.Project.Directory, Path.GetDirectoryName(file.Path)!);
        var depth = relative.Split(Path.DirectorySeparatorChar).Length;

        return relative != "." && depth >= 2 ? relative : string.Empty;
    }

    private static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private static IEnumerable<Type> Flatten(Type type)
    {
        var parts = type.HasElementType ? [type.GetElementType()!] : type.GetGenericArguments();

        return parts.SelectMany(Flatten).Prepend(type);
    }

    private static bool IsHttpOrCallerType(Type type) =>
        type == typeof(ClaimsPrincipal) ||
        type.Namespace?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true;
}
