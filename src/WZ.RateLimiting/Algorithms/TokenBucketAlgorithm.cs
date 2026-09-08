using WZ.RateLimiting.Abstractions;

namespace WZ.RateLimiting.Algorithms;

/// <summary>
/// 
/// </summary>
public class TokenBucketAlgorithm(IRateLimitStore store):IRateLimitAlgorithm
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async ValueTask<RateLimitDecision> EvaluateAsync(RateLimitContext context, CancellationToken cancellationToken)
    {
        var key = BuildKey(context.Policy.Name, context.IdentifierKey);
        
        var bucket = await store.IncrementBucketAsync(key,context.Policy.PermitLimit ,context.Policy.Window, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var elapsed = (now - bucket.WindowStart);
        var timeTravel = elapsed / context.Policy.Window;
        double newTokens = (timeTravel * context.Policy.RefillCapacity) ;
        int availableTokens  = (int) Math.Min(context.Policy.PermitLimit ,bucket.Count+ newTokens);
        bool isAllowed = availableTokens >= 1;
        if (isAllowed)
        {
            availableTokens  -= 1;
        }
        
        await store.UpdateAsync(key, new RateLimitCounterEntry(0,availableTokens ,now), cancellationToken);
        
        
        var remaining = Math.Min(0, (int)(context.Policy.PermitLimit - availableTokens ));
        var resetsAt = now+context.Policy.Window;
        return new RateLimitDecision(
            IsAllowed: isAllowed,
            Limit: context.Policy.PermitLimit,
            Remaining: remaining,
            ResetsAt: resetsAt,
            RetryAfter: isAllowed ? null : resetsAt - now);
    }
    
    private static string BuildKey(string policyName, string identifierKey) =>
        $"{policyName}:{identifierKey}";
}