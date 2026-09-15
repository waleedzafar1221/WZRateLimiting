using StackExchange.Redis;
using WZ.RateLimiting.Abstractions;

namespace WZ.RateLimiting.Redis;

/// <summary>
/// Redis-backed implementation of <see cref="IRateLimitStore"/> for
/// distributed rate limiting across multiple app instances.
/// </summary>
public sealed class RedisRateLimitStore : IRateLimitStore
{
    private readonly IConnectionMultiplexer _redis;
    
    private const string FixedAndSlideWindowScript = """
                                             local key = KEYS[1]
                                             local windowSeconds = tonumber(ARGV[1])
                                             local time = redis.call('TIME')
                                             local now = tonumber(time[1])
                                             local windowStart = redis.call('HGET', key, 'windowStart')
                                             local count = redis.call('HGET', key, 'count')
                                             local pcount = redis.call('HGET', key, 'pcount')
                                             
                                             if windowStart == false or (now - tonumber(windowStart)) >= windowSeconds then
                                                 pcount = count == false and 0 or tonumber(count)
                                                 windowStart = now
                                                 count = 0
                                             else
                                                 windowStart = tonumber(windowStart)
                                                 count = tonumber(count)
                                                 pcount = pcount == false and 0 or tonumber(pcount)
                                             end
                                             
                                             count = count + 1
                                             
                                             redis.call('HSET', key, 'windowStart', windowStart, 'count', count, 'pcount', pcount)
                                             redis.call('EXPIRE', key, windowSeconds * 2)
                                             
                                             return {count, pcount, windowStart}
                                             """;
    private const string IncrementBucketScript = """
                                                 local key = KEYS[1]
                                                 local windowSeconds = tonumber(ARGV[1])
                                                 local capacity = tonumber(ARGV[2])

                                                 local time = redis.call('TIME')
                                                 local now = tonumber(time[1])

                                                 local windowStart = redis.call('HGET', key, 'windowStart')
                                                 local count = redis.call('HGET', key, 'count')
                                                 local pcount = redis.call('HGET', key, 'pcount')

                                                 if windowStart == false then
                                                     windowStart = now
                                                     count = capacity
                                                     pcount = 0
                                                     redis.call('HSET', key, 'windowStart', windowStart, 'count', count, 'pcount', pcount)
                                                     redis.call('EXPIRE', key, windowSeconds * 2)
                                                 else
                                                     windowStart = tonumber(windowStart)
                                                     count = tonumber(count)
                                                     pcount = pcount == false and 0 or tonumber(pcount)
                                                 end

                                                 return {count, pcount, windowStart}
                                                 """;
    private const string UpdateScript = """
                                        local key = KEYS[1]
                                        local windowSeconds = tonumber(ARGV[1])
                                        local windowStart = tonumber(ARGV[2])
                                        local count = tonumber(ARGV[3])
                                        local pcount = tonumber(ARGV[4])

                                        redis.call('HSET', key,
                                            'windowStart', windowStart,
                                            'count', count,
                                            'pcount', pcount)

                                        redis.call('EXPIRE', key, windowSeconds * 2)

                                        return {count, pcount, windowStart}
                                        """;
    /// <summary>
    /// </summary>
    /// <param name="redis"></param>
    public RedisRateLimitStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <summary>
    /// </summary>
    /// <param name="key"></param>
    /// <param name="window"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async ValueTask<RateLimitCounterEntry> GetOrCreateAsync(
        string key, TimeSpan window, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();

        var windowStartValue = await db.HashGetAsync(key, "windowStart");
        var countValue = await db.HashGetAsync(key, "count");
        if (windowStartValue.IsNull)
        {
            // Nothing in Redis for this key yet — report an empty, fresh
            // window without writing anything. Redis doesn't need a value
            // to exist before it can be read; unlike a C# dictionary, there's
            // nothing to "insert" just to make a read possible.
            return new RateLimitCounterEntry(0,0, DateTimeOffset.UtcNow);
        }

        var windowStart = DateTimeOffset.FromUnixTimeSeconds((long)windowStartValue);
        var count = (int)countValue;

        return new RateLimitCounterEntry(0,count, windowStart);
    }

    /// <summary>
    /// </summary>
    /// <param name="key"></param>
    /// <param name="window"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async ValueTask<RateLimitCounterEntry> IncrementAsync(
        string key, TimeSpan window, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();

        // KEYS[1] and ARGV[1] in the script are supplied here, in order,
        // as plain arrays — this is the "raw" EVAL calling style, matching
        // exactly what you already tested via redis-cli.
        var redisKeys = new RedisKey[] { key };
        var redisArgs = new RedisValue[] { (long)window.TotalSeconds };

        RedisResult result = await db.ScriptEvaluateAsync(FixedAndSlideWindowScript, redisKeys, redisArgs);

        // The script returns a Lua table {count, windowStart}, which
        // StackExchange.Redis surfaces as a RedisResult[] here.
        var values = (RedisResult[])result!;

        int count = (int)(long)values[0];
        int pCount = (int)(long)values[1];
        long windowStartUnixSeconds = (long)values[2];
        

        var windowStart = DateTimeOffset.FromUnixTimeSeconds(windowStartUnixSeconds);

        return new RateLimitCounterEntry(pCount,count, windowStart);

    }

    /// <summary>
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public ValueTask<bool> GetAsync(string key)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// </summary>
    /// <param name="key"></param>
    /// <param name="entry"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async ValueTask<RateLimitCounterEntry> UpdateAsync(string key, RateLimitCounterEntry entry, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();

        var redisKeys = new RedisKey[] { key };
        var redisArgs = new RedisValue[]
        {
            // The interface doesn't pass a window to UpdateAsync, so we need one.
            // Options below — pick the one that matches your contract.
            (long)TimeSpan.FromMinutes(1).TotalSeconds,   // ⚠️ placeholder, see note
            entry.WindowStart.ToUnixTimeSeconds(),
            entry.Count,
            entry.PCount
        };

        RedisResult result = await db.ScriptEvaluateAsync(UpdateScript, redisKeys, redisArgs);

        var values = (RedisResult[])result!;

        int count  = (int)(long)values[0];
        int pCount = (int)(long)values[1];
        long windowStartUnixSeconds = (long)values[2];

        var windowStart = DateTimeOffset.FromUnixTimeSeconds(windowStartUnixSeconds);

        return new RateLimitCounterEntry(pCount, count, windowStart);
    }

    
    /// <summary>
    /// </summary>
    /// <param name="key"></param>
    /// <param name="capacity"></param>
    /// <param name="window"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async ValueTask<RateLimitCounterEntry> IncrementBucketAsync(string key, int capacity, TimeSpan window, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();

        var redisKeys = new RedisKey[] { key };
        var redisArgs = new RedisValue[]
        {
            (long)window.TotalSeconds,
            (long)capacity
        };

        RedisResult result = await db.ScriptEvaluateAsync(IncrementBucketScript, redisKeys, redisArgs);

        var values = (RedisResult[])result!;

        int count  = (int)(long)values[0];
        int pCount = (int)(long)values[1];
        long windowStartUnixSeconds = (long)values[2];

        var windowStart = DateTimeOffset.FromUnixTimeSeconds(windowStartUnixSeconds);

        return new RateLimitCounterEntry(pCount, count, windowStart);
    }
}