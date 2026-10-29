using Engine.Infrastructure.Namespaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class NamespaceListingTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task Listing_ShouldRequestTheNamespaces_WhenItStarts()
    {
        // Arrange
        var registry = new CountingRegistry(failures: 0);
        using var listing = Listing(registry, new FakeTimeProvider());

        // Act
        await listing.StartAsync(Ct);

        // Assert
        await registry.WaitForListsAsync(1, Ct);
        await listing.StopAsync(Ct);
        registry.Lists.ShouldBe(1);
    }

    [Fact]
    public async Task Listing_ShouldRequestTheNamespacesAgain_WhenOneIntervalPasses()
    {
        // Arrange
        var registry = new CountingRegistry(failures: 0);
        var time = new FakeTimeProvider();
        using var listing = Listing(registry, time);
        await listing.StartAsync(Ct);
        await registry.WaitForListsAsync(1, Ct);

        // Act
        time.Advance(Interval);

        // Assert
        await registry.WaitForListsAsync(2, Ct);
        await listing.StopAsync(Ct);
        registry.Lists.ShouldBe(2);
    }

    [Fact]
    public async Task Listing_ShouldKeepRequesting_WhenARequestThrows()
    {
        // Arrange
        var registry = new CountingRegistry(failures: 1);
        var time = new FakeTimeProvider();
        using var listing = Listing(registry, time);
        await listing.StartAsync(Ct);
        await registry.WaitForListsAsync(1, Ct);

        // Act
        time.Advance(Interval);

        // Assert
        await registry.WaitForListsAsync(2, Ct);
        await listing.StopAsync(Ct);
        registry.Lists.ShouldBe(2);
    }

    private static NamespaceListing Listing(IRegistryRequests registry, TimeProvider time) =>
        new(
            registry,
            Options.Create(new NamespaceListOptions { Interval = Interval }),
            time,
            NullLogger<NamespaceListing>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class CountingRegistry(int failures) : IRegistryRequests
    {
        private int _lists;
        public int Lists => _lists;

        public Task ListNamespacesAsync(CancellationToken cancellationToken) =>
            Interlocked.Increment(ref _lists) <= failures
                ? Task.FromException(new InvalidOperationException("The Registry could not be reached."))
                : Task.CompletedTask;

        public Task ResyncNamespaceAsync(string name, CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task WaitForListsAsync(int count, CancellationToken cancellationToken)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));

            while (Volatile.Read(ref _lists) < count)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
            }
        }
    }
}
