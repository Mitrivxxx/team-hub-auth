using System.Text.Json;
using StackExchange.Redis;
using team_hub_auth.Exceptions;

namespace team_hub_auth.Services.Sessions;

public sealed class RedisSessionStore(IConnectionMultiplexer redis, ILogger<RedisSessionStore> logger) : ISessionStore
{
    const string KeyPrefix = "auth:session:";

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
            await Database.StringSetAsync(BuildKey(refreshTokenHash), payload, ttl);
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

    public Task RevokeRefreshSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default) =>
        Database.KeyDeleteAsync(BuildKey(refreshTokenHash));

    static RedisKey BuildKey(string refreshTokenHash) => $"{KeyPrefix}{refreshTokenHash}";
}
