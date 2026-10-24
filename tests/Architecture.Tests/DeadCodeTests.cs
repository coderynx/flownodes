using Xunit;

namespace Architecture.Tests;

/// <summary>
///     Nothing is dead (ADR-0024): a type no other code references is deleted, contract types in
///     abstractions included.
/// </summary>
public sealed class DeadCodeTests
{
    [Fact]
    public void Unreferenced_ShouldBeEmpty_WhenEveryProductionTypeIsReferenced()
    {
        // Arrange
        var root = Solution.Root();

        // Act
        var dead = DeadCode.Unreferenced(root);

        // Assert
        dead.ShouldBeEmpty();
    }

    [Fact]
    public void Find_ShouldReport_WhenNoFileReferencesTheType()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["a/Lonely.cs"] = "public sealed class Lonely\n{\n}\n",
            ["b/Other.cs"] = "public sealed class Other\n{\n}\n"
        };

        // Act
        var dead = DeadCode.Find(sources);

        // Assert
        dead.Count.ShouldBe(2);
    }

    [Fact]
    public void Find_ShouldNotReport_WhenAnotherFileReferencesTheType()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["a/Used.cs"] = "public sealed class Used\n{\n}\n",
            ["b/User.cs"] = "public sealed class User\n{\n    private Used _used;\n}\n",
            ["c/Main.cs"] = "public static class Main\n{\n    public static User Make() => new User();\n}\n"
        };

        // Act
        var dead = DeadCode.Find(sources);

        // Assert
        dead.ShouldNotContain(entry => entry.Contains("Used"));
    }

    [Fact]
    public void Find_ShouldNotReport_WhenASiblingInTheSameFileReferencesTheType()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["a/Pair.cs"] =
                "public sealed class Pair\n{\n    public Half Left { get; }\n}\n\npublic sealed class Half\n{\n}\n",
            ["b/Main.cs"] = "public static class Main\n{\n    public static Pair Make() => new Pair();\n}\n"
        };

        // Act
        var dead = DeadCode.Find(sources);

        // Assert
        dead.ShouldNotContain(entry => entry.Contains("Half"));
    }

    [Fact]
    public void Find_ShouldNotCountAComment_WhenItNamesTheType()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["a/Ghost.cs"] = "public sealed class Ghost\n{\n}\n",
            ["b/Note.cs"] = "// Ghost is mentioned here\npublic sealed class Note\n{\n}\n"
        };

        // Act
        var dead = DeadCode.Find(sources);

        // Assert
        dead.ShouldContain(entry => entry.Contains("Ghost"));
    }

    [Theory]
    [InlineData("public sealed class ItemConfiguration : IEntityTypeConfiguration<Item>\n{\n}\n")]
    [InlineData("public static class ItemMappings\n{\n    public static int ToDto(this Item item) => 1;\n}\n")]
    [InlineData(
        "public static class ItemMappings\n{\n    public static int ToDto(\n        this Item item) => 1;\n}\n")]
    [InlineData("internal sealed class Handler : IRequestHandler<Query, Result>\n{\n}\n")]
    [InlineData("internal sealed class Handler : ICommandHandler<Command, Guid>\n{\n}\n")]
    [InlineData("internal sealed class Endpoint : IEndpoint\n{\n}\n")]
    [InlineData("internal sealed class Program\n{\n    private static void Main(string[] args)\n    {\n    }\n}\n")]
    public void Find_ShouldNotReport_WhenAFrameworkInvokesTheType(string source)
    {
        // Arrange
        var sources = new Dictionary<string, string> { ["a/Framework.cs"] = source };

        // Act
        var dead = DeadCode.Find(sources);

        // Assert
        dead.ShouldBeEmpty();
    }
}
