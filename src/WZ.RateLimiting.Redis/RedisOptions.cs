namespace WZ.RateLimiting.Redis;

/// <summary>
/// Configuration for connecting to Redis.
/// </summary>
public sealed class RedisOptions
{
    /// <summary>
    /// Redis connection string, e.g. "localhost:6379".
    /// </summary>
    public required string ConnectionString { get; init; }
}