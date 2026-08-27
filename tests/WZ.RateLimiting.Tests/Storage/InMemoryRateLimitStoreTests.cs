using WZ.RateLimiting.Storage;

namespace WZ.RateLimiting.Tests.Storage;

/// <summary>
/// 
/// </summary>
public class InMemoryRateLimitStoreTests
{
    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task IncrementAsync_FirstCall_ReturnsCountOne()
    {
        var store = new InMemoryRateLimitStore();

        var entry = await store.IncrementAsync("key1", TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.Equal(1, entry.Count);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task IncrementAsync_MultipleCalls_IncrementsSequentially()
    {
        var store = new InMemoryRateLimitStore();
        var window = TimeSpan.FromMinutes(1);

        await store.IncrementAsync("key1", window, CancellationToken.None);
        await store.IncrementAsync("key1", window, CancellationToken.None);
        var entry = await store.IncrementAsync("key1", window, CancellationToken.None);

        Assert.Equal(3, entry.Count);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task IncrementAsync_DifferentKeys_AreIndependent()
    {
        var store = new InMemoryRateLimitStore();
        var window = TimeSpan.FromMinutes(1);

        await store.IncrementAsync("ipA", window, CancellationToken.None);
        await store.IncrementAsync("ipA", window, CancellationToken.None);
        var entryA = await store.IncrementAsync("ipA", window, CancellationToken.None);

        var entryB = await store.IncrementAsync("ipB", window, CancellationToken.None);

        Assert.Equal(3, entryA.Count);
        Assert.Equal(1, entryB.Count);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task IncrementAsync_AfterWindowExpires_CounterResets()
    {
        var store = new InMemoryRateLimitStore();
        var shortWindow = TimeSpan.FromMilliseconds(100);

        await store.IncrementAsync("key1", shortWindow, CancellationToken.None);
        await store.IncrementAsync("key1", shortWindow, CancellationToken.None);

        await Task.Delay(150);

        var entry = await store.IncrementAsync("key1", shortWindow, CancellationToken.None);

        Assert.Equal(1, entry.Count);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task IncrementAsync_100ConcurrentCalls_CountsExactlyOneHundred()
    {
        var store = new InMemoryRateLimitStore();
        var window = TimeSpan.FromMinutes(1);

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => store.IncrementAsync("shared-key", window, CancellationToken.None).AsTask());

        await Task.WhenAll(tasks);

        var finalEntry = await store.GetOrCreateAsync("shared-key", window, CancellationToken.None);

        Assert.Equal(100, finalEntry.Count);
    }
}