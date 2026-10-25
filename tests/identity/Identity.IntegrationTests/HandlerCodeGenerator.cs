using JasperFx;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Xunit;

namespace Identity.IntegrationTests;

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
        await using var app = IdentityApp.Build("Host=x;Database=x", Convert.ToBase64String(new byte[32]));
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
               !File.Exists(Path.Combine(directory.FullName, "Identity.IntegrationTests.csproj")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The test project directory was not found.");
    }
}
