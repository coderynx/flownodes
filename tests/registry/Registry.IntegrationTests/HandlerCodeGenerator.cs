using JasperFx;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Xunit;

namespace Registry.IntegrationTests;

public sealed class HandlerCodeGenerator
{
    public static bool Enabled => Environment.GetEnvironmentVariable("FLOWNODES_WRITE_HANDLERS") == "1";

    [Fact(
        Skip = "Set FLOWNODES_WRITE_HANDLERS=1 to write the handler code.",
        SkipUnless = nameof(Enabled),
        SkipType = typeof(HandlerCodeGenerator))]
    public async Task Write_ShouldProduceHandlerCode_WhenEnabled()
    {
        // Arrange
        await using var app = RegistryApp.Build("Host=x;Database=x", "http://localhost");
        var options = app.Services.GetRequiredService<WolverineOptions>();
        options.CodeGeneration.GeneratedCodeOutputPath = Path.Combine(ProjectDirectory(), "Internal", "Generated");

        // Act
        var exitCode = await app.RunJasperFxCommands(["codegen", "write"]);

        // Assert
        exitCode.ShouldBe(0);
    }

    private static string ProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Registry.IntegrationTests.csproj")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The test project directory was not found.");
    }
}
