using System.Collections.Concurrent;

namespace team_hub_auth.Services.Sessions;

public sealed class InMemorySessionStore : ISessionStore
{
    readonly ConcurrentDictionary<string, (RefreshSession Session, DateTimeOffset ExpiresAt)> sessions = new();
    readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> userSessions = new();

    public Task StoreRefreshSessionAsync(
        string refreshTokenHash,
        Guid userId,
        bool rememberMe,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        sessions[refreshTokenHash] = (new RefreshSession(userId, rememberMe), expiresAt);
        userSessions.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>())[refreshTokenHash] = 0;
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
            RemoveSession(refreshTokenHash, entry.Session.UserId);
            return Task.FromResult<RefreshSession?>(null);
        }

        return Task.FromResult<RefreshSession?>(entry.Session);
    }

    public Task RevokeRefreshSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        if (sessions.TryGetValue(refreshTokenHash, out var entry))
            RemoveSession(refreshTokenHash, entry.Session.UserId);
        else
            sessions.TryRemove(refreshTokenHash, out _);

        return Task.CompletedTask;
    }

    public Task RevokeAllSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userSessions.TryRemove(userId, out var hashes))
        {
            foreach (var hash in hashes.Keys)
                sessions.TryRemove(hash, out _);
        }

        return Task.CompletedTask;
    }

    void RemoveSession(string refreshTokenHash, Guid userId)
    {
        sessions.TryRemove(refreshTokenHash, out _);
        if (userSessions.TryGetValue(userId, out var hashes))
        {
            hashes.TryRemove(refreshTokenHash, out _);
            if (hashes.IsEmpty)
                userSessions.TryRemove(userId, out _);
        }
    }
}
