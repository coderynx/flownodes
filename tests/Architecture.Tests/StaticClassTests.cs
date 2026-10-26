using Xunit;

namespace Architecture.Tests;

/// <summary>
///     Services are injected and static classes stay pure (ADR-0028).
/// </summary>
public sealed class StaticClassTests
{
    private const string Contract = "public interface ISecretVault\n{\n}\n";

    [Fact]
    public void Impure_ShouldBeEmpty_WhenEveryStaticClassIsPure()
    {
        // Arrange
        var root = Solution.Root();

        // Act
        var impure = StaticClasses.Impure(root);

        // Assert
        impure.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("internal static class Storage\n{\n    public static void Store(ISecretVault vault) { }\n}\n")]
    [InlineData(
        "internal static class Storage\n{\n    public static Task StoreAsync(\n        this ISecretVault vault,\n" +
        "        string key)\n    {\n        return Task.CompletedTask;\n    }\n}\n")]
    [InlineData(
        "internal static class Storage\n{\n    private static void Store(int keep, ISecretVault vault) { }\n}\n")]
    [InlineData(
        "internal static class Storage\n{\n    public static Task<(int A, int B)> StoreAsync(ISecretVault vault) =>" +
        " Task.FromResult((1, 2));\n}\n")]
    public void Find_ShouldReport_WhenAStaticMethodTakesASolutionInterface(string source)
    {
        // Arrange
        var sources = new Dictionary<string, string> { ["a/ISecretVault.cs"] = Contract, ["b/Storage.cs"] = source };

        // Act
        var impure = StaticClasses.Find(sources);

        // Assert
        impure.ShouldHaveSingleItem().ShouldContain("Storage.Store");
    }

    [Theory]
    [InlineData("TimeProvider time")]
    [InlineData("UserManager<User> users")]
    [InlineData("ISender sender")]
    [InlineData("IGrainFactory grains")]
    public void Find_ShouldReport_WhenAStaticMethodTakesAFrameworkService(string parameter)
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["a/Helper.cs"] = $"internal static class Helper\n{{\n    public static void Run({parameter}) {{ }}\n}}\n"
        };

        // Act
        var impure = StaticClasses.Find(sources);

        // Assert
        impure.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("DateTimeOffset.UtcNow")]
    [InlineData("DateTime.Now")]
    [InlineData("TimeProvider.System")]
    [InlineData("RandomNumberGenerator.GetBytes(32)")]
    [InlineData("Guid.NewGuid()")]
    public void Find_ShouldReport_WhenAStaticClassReadsAnAmbientSource(string read)
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["a/Helper.cs"] = $"internal static class Helper\n{{\n    public static object Make() => {read};\n}}\n"
        };

        // Act
        var impure = StaticClasses.Find(sources);

        // Assert
        impure.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(
        "internal static class Checks\n{\n    public static bool Fits(JsonElement json, IReadOnlyList<string> v) =>" +
        " true;\n}\n")]
    [InlineData(
        "internal static class Columns\n{\n    public static PropertyBuilder<JsonElement> IsJsonb(\n" +
        "        this PropertyBuilder<JsonElement> property) => property;\n}\n")]
    [InlineData("internal static class Defaults\n{\n    private static readonly ISecretVault Vault = new(\"x\");\n}\n")]
    [InlineData(
        "internal sealed class Storage(ISecretVault vault)\n{\n    public void Store() => vault.ToString();\n}\n")]
    [InlineData("internal sealed class Ticket\n{\n    public static Ticket Create() => new(Guid.NewGuid());\n}\n")]
    public void Find_ShouldSkip_WhenTheCodeIsPureOrNotAStaticClass(string source)
    {
        // Arrange
        var sources = new Dictionary<string, string> { ["a/ISecretVault.cs"] = Contract, ["b/Code.cs"] = source };

        // Act
        var impure = StaticClasses.Find(sources);

        // Assert
        impure.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(
        "m/Bootstrap/DependencyInjection.cs",
        "public static class DependencyInjection\n{\n    public static void Add(IServiceProvider services) =>" +
        " Use(TimeProvider.System);\n}\n")]
    [InlineData(
        "m/Messaging/IdentityMessageHandler.cs",
        "public static class IdentityMessageHandler\n{\n    public static Task Handle(RoleGranted m, ISender s) =>" +
        " Task.CompletedTask;\n}\n")]
    [InlineData(
        "m/Connect/TokenEndpoint.cs",
        "internal static class TokenEndpoint\n{\n    public static void Map(WebApplication app) =>" +
        " app.MapPost(\"/t\", HandleAsync);\n    private static Task HandleAsync(ISender sender) =>" +
        " Task.CompletedTask;\n}\n")]
    public void Find_ShouldSkip_WhenTheFrameworkInjectsTheParameters(string file, string source)
    {
        // Arrange
        var sources = new Dictionary<string, string> { [file] = source };

        // Act
        var impure = StaticClasses.Find(sources);

        // Assert
        impure.ShouldBeEmpty();
    }
}
