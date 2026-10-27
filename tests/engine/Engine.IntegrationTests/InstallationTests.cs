using Engine.Infrastructure.Installations;
using Npgsql;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class InstallationTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task StartAsync_ShouldGenerateAVersion4Identifier_WhenTheDatabaseIsEmpty()
    {
        // Arrange
        await using var database = NpgsqlDataSource.Create(await postgres.CreateDatabaseAsync());
        var silo = new StoredInstallation(database);

        // Act
        await silo.StartAsync(Ct);

        // Assert
        silo.Current.Id.Version.ShouldBe(4);
    }

    [Fact]
    public async Task StartAsync_ShouldKeepTheIdentifier_WhenTheSiloRestarts()
    {
        // Arrange
        await using var database = NpgsqlDataSource.Create(await postgres.CreateDatabaseAsync());
        var first = new StoredInstallation(database);
        await first.StartAsync(Ct);

        // Act
        var restarted = new StoredInstallation(database);
        await restarted.StartAsync(Ct);

        // Assert
        restarted.Current.Id.ShouldBe(first.Current.Id);
    }

    [Fact]
    public async Task StartAsync_ShouldGiveEverySiloTheSameIdentifier_WhenSilosStartAtOnce()
    {
        // Arrange
        await using var database = NpgsqlDataSource.Create(await postgres.CreateDatabaseAsync());
        var silos = Enumerable.Range(0, 8).Select(_ => new StoredInstallation(database)).ToList();

        // Act
        await Task.WhenAll(silos.Select(silo => silo.StartAsync(Ct)));

        // Assert
        silos.Select(silo => silo.Current.Id).Distinct().Count().ShouldBe(1);
    }
}
