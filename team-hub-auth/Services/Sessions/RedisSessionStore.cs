using System.Text.Json;
using StackExchange.Redis;
using team_hub_auth.Exceptions;

namespace team_hub_auth.Services.Sessions;

public sealed class RedisSessionStore(IConnectionMultiplexer redis, ILogger<RedisSessionStore> logger) : ISessionStore
{
    const string KeyPrefix = "auth:session:";
    const string UserSessionsKeyPrefix = "auth:user-sessions:";

    IDatabase Database => redis.GetDatabase();

    public async Task StoreRefreshSessionAsync(
        string refreshTokenHash,
        Guid userId,
        bool rememberMe,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var ttl = expiresAt - DateTimeOffset.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            logger.LogWarning("Skipped storing expired refresh session for user {UserId}", userId);
            return;
        }

        try
        {
            var payload = JsonSerializer.Serialize(new RefreshSession(userId, rememberMe));
            var sessionKey = BuildKey(refreshTokenHash);
            var userSessionsKey = BuildUserSessionsKey(userId);

            var ttlSeconds = (int)Math.Ceiling(ttl.TotalSeconds);
            // Atomically store session + index membership, and keep the SET TTL at least as long as this session.
            const string script = @"
                redis.call('SET', KEYS[1], ARGV[1], 'EX', ARGV[2])
                redis.call('SADD', KEYS[2], ARGV[3])
                local currentTtl = redis.call('TTL', KEYS[2])
                if currentTtl < tonumber(ARGV[2]) then
                  redis.call('EXPIRE', KEYS[2], ARGV[2])
                end
                return 1
            ";

            await Database.ScriptEvaluateAsync(
                script,
                [sessionKey, userSessionsKey],
                [payload, ttlSeconds, refreshTokenHash]);
        }
        catch (RedisException ex)
        {
            logger.LogError(ex, "Redis session store unavailable");
            throw new RedisUnavailableException("Redis session store is unavailable.", ex);
        }
    }

    public async Task<RefreshSession?> GetRefreshSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await Database.StringGetAsync(BuildKey(refreshTokenHash));
            if (value.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<RefreshSession>((string)value!);
        }
        catch (RedisException ex)
        {
            logger.LogError(ex, "Redis session store unavailable");
            throw new RedisUnavailableException("Redis session store is unavailable.", ex);
        }
    }

    public async Task RevokeRefreshSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionKey = BuildKey(refreshTokenHash);
            var value = await Database.StringGetAsync(sessionKey);
            if (!value.IsNullOrEmpty)
            {
                var session = JsonSerializer.Deserialize<RefreshSession>((string)value!);
                if (session is not null)
                    await Database.SetRemoveAsync(BuildUserSessionsKey(session.UserId), refreshTokenHash);
            }

            await Database.KeyDeleteAsync(sessionKey);
        }
        catch (RedisException ex)
        {
            logger.LogError(ex, "Redis session store unavailable");
            throw new RedisUnavailableException("Redis session store is unavailable.", ex);
        }
    }

    public async Task RevokeAllSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userSessionsKey = BuildUserSessionsKey(userId);
            var hashes = await Database.SetMembersAsync(userSessionsKey);
            if (hashes.Length > 0)
            {
                var keys = hashes
                    .Select(h => (RedisKey)BuildKey((string)h!))
                    .Append(userSessionsKey)
                    .ToArray();
                await Database.KeyDeleteAsync(keys);
            }
            else
            {
                await Database.KeyDeleteAsync(userSessionsKey);
            }
        }
        catch (RedisException ex)
        {
            logger.LogError(ex, "Redis session store unavailable");
            throw new RedisUnavailableException("Redis session store is unavailable.", ex);
        }
    }

    static RedisKey BuildKey(string refreshTokenHash) => $"{KeyPrefix}{refreshTokenHash}";

    static RedisKey BuildUserSessionsKey(Guid userId) => $"{UserSessionsKeyPrefix}{userId:D}";
}
