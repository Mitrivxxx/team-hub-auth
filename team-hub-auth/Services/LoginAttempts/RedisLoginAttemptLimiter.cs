using StackExchange.Redis;
using TeamHub.Redis;
using team_hub_auth.Exceptions;

namespace team_hub_auth.Services.LoginAttempts;

public sealed class RedisLoginAttemptLimiter(
    IConnectionMultiplexer redis,
    IRedisKeySegmenter keySegmenter,
    ILogger<RedisLoginAttemptLimiter> logger) : ILoginAttemptLimiter
{
    IDatabase Database => redis.GetDatabase();

    public Task<LoginLockoutStatus> GetLockoutStatusAsync(string username, CancellationToken cancellationToken = default)
    {
        var lockoutKey = keySegmenter.BuildSegmentedKey(RedisDataSegments.AuthLoginLockout, username);

        return GetLockoutStatusCore(lockoutKey, cancellationToken);
    }

    async Task<LoginLockoutStatus> GetLockoutStatusCore(RedisKey lockoutKey, CancellationToken cancellationToken)
    {
        try
        {
            var ttl = await Database.KeyTimeToLiveAsync(lockoutKey);
            if (ttl is null || ttl <= TimeSpan.Zero)
                return new(IsLocked: false, LockoutSeconds: 0);

            return new(IsLocked: true, LockoutSeconds: (int)Math.Ceiling(ttl.Value.TotalSeconds));
        }
        catch (RedisException ex)
        {
            logger.LogError(ex, "Redis login lockout store unavailable");
            throw new RedisUnavailableException("Redis login lockout store is unavailable.", ex);
        }
    }

    public Task<LoginFailureOutcome> RegisterFailedAttemptAsync(
        string username,
        int maxFailedAttempts,
        TimeSpan lockoutDuration,
        CancellationToken cancellationToken = default)
    {
        if (maxFailedAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFailedAttempts));
        }

        if (lockoutDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lockoutDuration));
        }

        var attemptsKey = keySegmenter.BuildSegmentedKey(RedisDataSegments.AuthLoginAttempts, username);
        var lockoutKey = keySegmenter.BuildSegmentedKey(RedisDataSegments.AuthLoginLockout, username);

        var lockoutSeconds = (int)Math.Ceiling(lockoutDuration.TotalSeconds);

        // Atomically:
        // 1) INCR attempts key
        // 2) Set attempts key TTL on first increment
        // 3) When attempts >= max: set lockout key with TTL, delete attempts key
        // Returns: attempts, remainingAttempts, isLocked, lockoutSeconds
        const string script = @"
            local attempts = redis.call('INCR', KEYS[1])
            if attempts == 1 then
              redis.call('EXPIRE', KEYS[1], ARGV[1])
            end

            local maxFailedAttempts = tonumber(ARGV[2])
            local remainingAttempts = maxFailedAttempts - attempts
            if attempts >= maxFailedAttempts then
              redis.call('SET', KEYS[2], '1', 'EX', ARGV[1])
              redis.call('DEL', KEYS[1])
              return { attempts, 0, 1, ARGV[1] }
            end

            if remainingAttempts < 0 then remainingAttempts = 0 end
            return { attempts, remainingAttempts, 0, 0 }
        ";

        return RegisterFailedAttemptCoreAsync(
            username,
            maxFailedAttempts,
            lockoutSeconds,
            attemptsKey,
            lockoutKey,
            script,
            cancellationToken);
    }

    async Task<LoginFailureOutcome> RegisterFailedAttemptCoreAsync(
        string username,
        int maxFailedAttempts,
        int lockoutSeconds,
        string attemptsKey,
        string lockoutKey,
        string script,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await Database.ScriptEvaluateAsync(
                script,
                new RedisKey[] { attemptsKey, lockoutKey },
                new RedisValue[] { lockoutSeconds, maxFailedAttempts });

            var values = (RedisResult[])result!;
            var remainingAttempts = (int)(long)values[1];
            var isLocked = (long)values[2] == 1;
            var lockout = (int)(long)values[3];

            return new LoginFailureOutcome(
                RemainingAttempts: remainingAttempts,
                IsLocked: isLocked,
                LockoutSeconds: isLocked ? lockout : 0);
        }
        catch (RedisException ex)
        {
            logger.LogError(ex, "Redis login attempt limiter failed for username {Username}", username);
            throw new RedisUnavailableException("Redis login attempt limiter is unavailable.", ex);
        }
    }

    public async Task ClearAttemptsAsync(string username, CancellationToken cancellationToken = default)
    {
        var attemptsKey = keySegmenter.BuildSegmentedKey(RedisDataSegments.AuthLoginAttempts, username);
        var lockoutKey = keySegmenter.BuildSegmentedKey(RedisDataSegments.AuthLoginLockout, username);

        try
        {
            await Database.KeyDeleteAsync(attemptsKey);
            await Database.KeyDeleteAsync(lockoutKey);
        }
        catch (RedisException ex)
        {
            logger.LogError(ex, "Redis login attempt store unavailable");
            throw new RedisUnavailableException("Redis login attempt store is unavailable.", ex);
        }
    }
}

