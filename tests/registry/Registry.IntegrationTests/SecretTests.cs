using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Registry.IntegrationTests;

public sealed class SecretTests(PostgresFixture postgres)
{
    private const string Secrets = "/api/v1/registry/namespaces/acme/secrets";

    private const string Database = """
                                    apiVersion: flownodes.io/v1alpha1
                                    kind: Secret
                                    metadata:
                                      name: database
                                    spec:
                                      data:
                                        region: eu-west
                                        token: hunter2-correct-horse
                                    """;

    [Fact]
    public async Task Get_ShouldReturnKeyNamesOnly_WhenTheSecretWasApplied()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        (await RegistryApp.ApplyAsync(engineer, Database, Ct, "?namespace=acme")).EnsureSuccessStatusCode();

        // Act
        var one = await engineer.GetStringAsync($"{Secrets}/database", Ct);
        var list = await engineer.GetStringAsync(Secrets, Ct);
        var revisions = await engineer.GetStringAsync($"{Secrets}/database/revisions", Ct);

        // Assert
        JsonDocument.Parse(one)
            .RootElement.GetProperty("spec")
            .GetRawText()
            .ShouldBe("""{"keys":["region","token"]}""");
        foreach (var body in new[] { one, list, revisions })
        {
            body.ShouldNotContain("hunter2");
            body.ShouldNotContain("eu-west");
        }
    }

    [Fact]
    public async Task Apply_ShouldKeepValuesOutOfSpecsRevisionsAndMessagesAndEncryptThemAtRest_WhenASecretIsApplied()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);

        // Act
        var response = await RegistryApp.ApplyAsync(engineer, Database, Ct, "?namespace=acme");

        // Assert
        var body = await response.Content.ReadAsStringAsync(Ct);
        body.ShouldNotContain("hunter2");
        var applied = await app.Messages.WaitForAppliedAsync("database", Ct);
        applied.Spec.GetRawText().ShouldNotContain("hunter2");

        await using var connection = await OpenAsync(app);
        (await ReadTextAsync(connection, """SELECT "Spec"::text FROM resources WHERE "Name" = 'database'"""))
            .ShouldNotContain("hunter2");
        (await ReadTextAsync(connection, """SELECT "Spec"::text FROM revisions WHERE "Name" = 'database'"""))
            .ShouldNotContain("hunter2");
        var encrypted = await ReadBytesAsync(
            connection,
            """SELECT "Encrypted" FROM secret_values WHERE "Key" = 'token'""");
        encrypted.Length.ShouldBeGreaterThan(0);
        Encoding.UTF8.GetString(encrypted).ShouldNotContain("hunter2");
        encrypted.AsSpan().IndexOf("hunter2"u8).ShouldBe(-1);
    }

    [Fact]
    public async Task Apply_ShouldStartAGenerationWithoutShowingValues_WhenAValueChanges()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, Database, Ct, "?namespace=acme");

        // Act
        var response = await RegistryApp.ApplyAsync(
            engineer,
            Database.Replace("hunter2-correct-horse", "battery-staple"),
            Ct,
            "?namespace=acme&dryRun=true");
        var dryRun = await response.Content.ReadAsStringAsync(Ct);
        var applied = await RegistryApp.ApplyAsync(
            engineer,
            Database.Replace("hunter2-correct-horse", "battery-staple"),
            Ct,
            "?namespace=acme");

        // Assert
        dryRun.ShouldContain("(hidden)");
        dryRun.ShouldNotContain("hunter2");
        dryRun.ShouldNotContain("battery-staple");
        (await applied.Content.ReadAsStringAsync(Ct)).ShouldNotContain("battery-staple");
        var stored = await engineer.GetFromJsonAsync<JsonElement>($"{Secrets}/database", Ct);
        stored.GetProperty("metadata").GetProperty("generation").GetInt64().ShouldBe(2);
        await using var connection = await OpenAsync(app);
        (await ReadBytesAsync(connection, """SELECT "Encrypted" FROM secret_values WHERE "Key" = 'token'""")).AsSpan()
            .IndexOf("battery-staple"u8)
            .ShouldBe(-1);
    }

    [Fact]
    public async Task Apply_ShouldLeaveTheGenerationAlone_WhenTheSameValuesAreAppliedAgain()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, Database, Ct, "?namespace=acme");

        // Act
        var again = await RegistryApp.ApplyAsync(engineer, Database, Ct, "?namespace=acme");

        // Assert
        (await again.Content.ReadAsStringAsync(Ct)).ShouldContain("unchanged");
    }

    [Fact]
    public async Task Apply_ShouldDropAValue_WhenItsKeyLeavesTheSecret()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, Database, Ct, "?namespace=acme");

        // Act
        await RegistryApp.ApplyAsync(
            engineer,
            Database.Replace("    region: eu-west\n", string.Empty, StringComparison.Ordinal),
            Ct,
            "?namespace=acme");

        // Assert
        await using var connection = await OpenAsync(app);
        (await ReadTextAsync(connection, """SELECT string_agg("Key", ',') FROM secret_values""")).ShouldBe("token");
    }

    [Fact]
    public async Task Apply_ShouldRejectTheManifest_WhenAWidgetReferencesAMissingSecret()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);

        // Act
        var response = await RegistryApp.ApplyAsync(engineer, WidgetUsing("database"), Ct, "?namespace=acme");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("Secret 'database'");
    }

    [Fact]
    public async Task Apply_ShouldRejectTheManifest_WhenTheSecretLivesInAnotherNamespace()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, Database, Ct, "?namespace=acme");
        var elsewhere = WidgetUsing("database")
            .Replace("namespace: acme", "namespace: default", StringComparison.Ordinal);

        // Act
        var response = await RegistryApp.ApplyAsync(app.Client, elsewhere, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("does not exist in Namespace 'default'");
    }

    [Fact]
    public async Task Delete_ShouldRefuseTheSecretThenRemoveItsValues_WhenAWidgetReferencedIt()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, Database, Ct, "?namespace=acme");
        await RegistryApp.ApplyAsync(engineer, WidgetUsing("database"), Ct, "?namespace=acme");

        // Act
        var refused = await engineer.DeleteAsync($"{Secrets}/database", Ct);
        await engineer.DeleteAsync("/api/v1/registry/namespaces/acme/widgets/w-1", Ct);
        var deleted = await engineer.DeleteAsync($"{Secrets}/database", Ct);

        // Assert
        refused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        deleted.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var connection = await OpenAsync(app);
        (await ReadTextAsync(connection, "SELECT count(*)::text FROM secret_values")).ShouldBe("0");
    }

    private static string WidgetUsing(string secret) =>
        $"""
         apiVersion: flownodes.io/v1alpha1
         kind: Widget
         metadata:
           name: w-1
           namespace: acme
         spec:
           secret: {secret}
         """;

    private static async Task<NpgsqlConnection> OpenAsync(RegistryApp app)
    {
        var connection = new NpgsqlConnection(
            app.Services.GetRequiredService<IConfiguration>().GetConnectionString("registry"));
        await connection.OpenAsync(Ct);

        return connection;
    }

    private static async Task<string> ReadTextAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);

        return (string)(await command.ExecuteScalarAsync(Ct))!;
    }

    private static async Task<byte[]> ReadBytesAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);

        return (byte[])(await command.ExecuteScalarAsync(Ct))!;
    }

    private Task<RegistryApp> StartAsync() => RegistryApp.StartAsync(postgres);

    private static async Task<HttpClient> EngineerAsync(RegistryApp app)
    {
        await app.CreateNamespaceAsync("acme", Ct);
        var (_, engineer) = await app.MemberAsync("erin", "Engineer", "acme");
        await RegistryApp.EventuallyAsync(async () => (await engineer.GetAsync(Secrets, Ct)).IsSuccessStatusCode, Ct);

        return engineer;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
