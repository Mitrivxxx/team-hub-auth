namespace team_hub_auth.Services.Sessions;

public interface ISessionStore
{
    Task StoreRefreshSessionAsync(
        string refreshTokenHash,
        Guid userId,
        bool rememberMe,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    Task<RefreshSession?> GetRefreshSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task RevokeRefreshSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task RevokeAllSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
