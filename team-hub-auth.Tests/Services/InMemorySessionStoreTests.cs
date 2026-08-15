using team_hub_auth.Services.Sessions;

namespace team_hub_auth.Tests.Services;

public class InMemorySessionStoreTests
{
    [Fact]
    public async Task RevokeAllSessionsAsync_RemovesOnlyTargetUserSessions()
    {
        var store = new InMemorySessionStore();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        await store.StoreRefreshSessionAsync("hash-a1", userA, rememberMe: true, expiresAt);
        await store.StoreRefreshSessionAsync("hash-a2", userA, rememberMe: false, expiresAt);
        await store.StoreRefreshSessionAsync("hash-b1", userB, rememberMe: true, expiresAt);

        await store.RevokeAllSessionsAsync(userA);

        Assert.Null(await store.GetRefreshSessionAsync("hash-a1"));
        Assert.Null(await store.GetRefreshSessionAsync("hash-a2"));
        Assert.NotNull(await store.GetRefreshSessionAsync("hash-b1"));
    }

    [Fact]
    public async Task RevokeRefreshSessionAsync_RemovesIndexEntry()
    {
        var store = new InMemorySessionStore();
        var userId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        await store.StoreRefreshSessionAsync("hash-1", userId, rememberMe: true, expiresAt);
        await store.StoreRefreshSessionAsync("hash-2", userId, rememberMe: true, expiresAt);
        await store.RevokeRefreshSessionAsync("hash-1");

        Assert.Null(await store.GetRefreshSessionAsync("hash-1"));
        Assert.NotNull(await store.GetRefreshSessionAsync("hash-2"));

        await store.RevokeAllSessionsAsync(userId);
        Assert.Null(await store.GetRefreshSessionAsync("hash-2"));
    }
}
