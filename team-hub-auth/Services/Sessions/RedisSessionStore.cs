using System.Text.Json;
using StackExchange.Redis;

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

        var payload = JsonSerializer.Serialize(new RefreshSession(userId, rememberMe));
        await Database.StringSetAsync(BuildKey(refreshTokenHash), payload, ttl);
    }

    public async Task<RefreshSession?> GetRefreshSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        var value = await Database.StringGetAsync(BuildKey(refreshTokenHash));
        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<RefreshSession>((string)value!);
    }

    public Task RevokeRefreshSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default) =>
        Database.KeyDeleteAsync(BuildKey(refreshTokenHash));

    static RedisKey BuildKey(string refreshTokenHash) => $"{KeyPrefix}{refreshTokenHash}";
}
