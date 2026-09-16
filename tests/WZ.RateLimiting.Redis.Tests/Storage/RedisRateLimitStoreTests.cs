using StackExchange.Redis;

namespace WZ.RateLimiting.Redis.Tests.Storage;

/// <summary>
/// 
/// </summary>
public class RedisRateLimitStoreTests : IAsyncLifetime
{
    private IConnectionMultiplexer _redis = null!;
    private RedisRateLimitStore _store = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
        _store = new RedisRateLimitStore(_redis);
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        _redis.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task CheckRedisIsRunning()
    {
        var db = _redis.GetDatabase();
        var key = $"smoke-test:{Guid.NewGuid()}";

        await db.StringSetAsync(key, "hello");
        var value = await db.StringGetAsync(key);

        Assert.Equal("hello", value);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task IncrementAsync_FirstCall_ReturnsCountOne()
    {
        var key = $"test:{Guid.NewGuid()}";

        var entry = await _store.IncrementAsync(key, TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.Equal(1, entry.Count);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task IncrementAsync_MultipleCalls_IncrementsSequentially()
    {
        var key = $"test:{Guid.NewGuid()}";
        var window = TimeSpan.FromMinutes(1);

        await _store.IncrementAsync(key, window, CancellationToken.None);
        await _store.IncrementAsync(key, window, CancellationToken.None);
        var entry = await _store.IncrementAsync(key, window, CancellationToken.None);

        Assert.Equal(3, entry.Count);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task IncrementAsync_DifferentKeys_AreIndependent()
    {
        var keyA = $"test:{Guid.NewGuid()}";
        var keyB = $"test:{Guid.NewGuid()}";
        var window = TimeSpan.FromMinutes(1);

        await _store.IncrementAsync(keyA, window, CancellationToken.None);
        await _store.IncrementAsync(keyA, window, CancellationToken.None);
        var entryA = await _store.IncrementAsync(keyA, window, CancellationToken.None);

        var entryB = await _store.IncrementAsync(keyB, window, CancellationToken.None);

        Assert.Equal(3, entryA.Count);
        Assert.Equal(1, entryB.Count);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task IncrementAsync_AfterWindowExpires_CounterResets()
    {
        var key = $"test:{Guid.NewGuid()}";
        var shortWindow = TimeSpan.FromSeconds(2);

        await _store.IncrementAsync(key, shortWindow, CancellationToken.None);
        await _store.IncrementAsync(key, shortWindow, CancellationToken.None);

        await Task.Delay(shortWindow + TimeSpan.FromSeconds(1));

        var entry = await _store.IncrementAsync(key, shortWindow, CancellationToken.None);

        Assert.Equal(1, entry.Count);
    }
}