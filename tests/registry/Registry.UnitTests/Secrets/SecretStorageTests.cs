using System.Text;
using Registry.Application.Database;
using Registry.Application.Resources;
using Registry.Application.Secrets;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Registry.UnitTests.Resources;
using Xunit;

namespace Registry.UnitTests.Secrets;

public sealed class SecretStorageTests
{
    private static readonly NamespaceName Acme = NamespaceName.Create("acme");
    private static readonly ResourceName Db = ResourceName.Create("db");

    [Fact]
    public async Task StoreAsync_ShouldAddEncryptedValues_WhenTheSecretIsNew()
    {
        // Arrange
        var database = TestDatabase.Create();

        // Act
        await Storage(database).StoreAsync(Entry(("password", "hunter2")), Ct);

        // Assert
        database.SecretValues.Received(1)
            .Add(
                Arg.Is<SecretValue>(value =>
                    value.Key == "password" &&
                    value.Encrypted.SequenceEqual(Encoding.UTF8.GetBytes("encrypted-hunter2"))));
    }

    [Fact]
    public async Task StoreAsync_ShouldReplaceTheHeldValue_WhenTheKeyExists()
    {
        // Arrange
        var held = SecretValue.Create(Acme, Db, "password", [1]);
        var database = TestDatabase.Create(secretValues: [held]);

        // Act
        await Storage(database).StoreAsync(Entry(("password", "new")), Ct);

        // Assert
        held.Encrypted.ShouldBe(Encoding.UTF8.GetBytes("encrypted-new"));
        database.SecretValues.DidNotReceive().Add(Arg.Any<SecretValue>());
    }

    [Fact]
    public async Task StoreAsync_ShouldRemoveHeldValues_WhenTheirKeysAreGone()
    {
        // Arrange
        var stale = SecretValue.Create(Acme, Db, "old", [1]);
        var database = TestDatabase.Create(secretValues: [stale]);

        // Act
        await Storage(database).StoreAsync(Entry(("password", "new")), Ct);

        // Assert
        database.SecretValues.Received(1)
            .RemoveRange(Arg.Is<IEnumerable<SecretValue>>(removed => removed.SequenceEqual(new[] { stale })));
    }

    [Fact]
    public async Task ForgetAsync_ShouldRemoveOnlyTheSecretsValues_WhenTheSecretIsDeleted()
    {
        // Arrange
        var value = SecretValue.Create(Acme, Db, "password", [1]);
        var other = SecretValue.Create(Acme, ResourceName.Create("other"), "password", [2]);
        var database = TestDatabase.Create(secretValues: [value, other]);

        // Act
        await Storage(database).ForgetAsync(Entry(), Ct);

        // Assert
        database.SecretValues.Received(1)
            .RemoveRange(Arg.Is<IEnumerable<SecretValue>>(values => values.SequenceEqual(new[] { value })));
    }

    private static SecretStorage Storage(IApplicationDbContext database) => new(database, ResourceHandlers.Vault());

    private static ManifestEntry Entry(params (string Key, string Value)[] values) =>
        new(
            0,
            SecretKind.KindName,
            Db,
            Acme,
            null,
            Widgets.Json("{}"),
            null,
            values.ToDictionary(pair => pair.Key, pair => pair.Value));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
