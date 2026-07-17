using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace team_hub_auth.Configuration;

public sealed class RedisHealthCheck(IConnectionMultiplexer redis, ILogger<RedisHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = redis.GetDatabase();
            _ = await database.PingAsync();
            return HealthCheckResult.Healthy();
        }
        catch (RedisException ex)
        {
            logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy("Redis is unavailable.", ex);
        }
    }
}
