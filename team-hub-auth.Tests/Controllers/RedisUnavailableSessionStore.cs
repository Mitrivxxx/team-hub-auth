using StackExchange.Redis;
using team_hub_auth.Exceptions;
using team_hub_auth.Services.Sessions;

namespace team_hub_auth.Tests.Controllers;

internal sealed class RedisUnavailableSessionStore : ISessionStore
{
    static RedisUnavailableException CreateException() =>
        new("Redis session store is unavailable.", new RedisConnectionException(
            StackExchange.Redis.ConnectionFailureType.UnableToConnect,
            "Redis is unavailable."));

    public Task StoreRefreshSessionAsync(
        string refreshTokenHash,
        Guid userId,
        bool rememberMe,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default) =>
        throw CreateException();

    public Task<RefreshSession?> GetRefreshSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default) =>
        throw CreateException();

    public Task RevokeRefreshSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
