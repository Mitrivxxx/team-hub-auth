using System.Collections.Concurrent;

namespace team_hub_auth.Services.Sessions;

public sealed class InMemorySessionStore : ISessionStore
{
    readonly ConcurrentDictionary<string, (RefreshSession Session, DateTimeOffset ExpiresAt)> sessions = new();

    public Task StoreRefreshSessionAsync(
        string refreshTokenHash,
        Guid userId,
        bool rememberMe,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        sessions[refreshTokenHash] = (new RefreshSession(userId, rememberMe), expiresAt);
        return Task.CompletedTask;
    }

    public Task<RefreshSession?> GetRefreshSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        if (!sessions.TryGetValue(refreshTokenHash, out var entry))
            return Task.FromResult<RefreshSession?>(null);

        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            sessions.TryRemove(refreshTokenHash, out _);
            return Task.FromResult<RefreshSession?>(null);
        }

        return Task.FromResult<RefreshSession?>(entry.Session);
    }

    public Task RevokeRefreshSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        sessions.TryRemove(refreshTokenHash, out _);
        return Task.CompletedTask;
    }
}
