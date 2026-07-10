using Testcontainers.Redis;

namespace team_hub_auth.Tests.Integration;

public sealed class RedisIntegrationFixture : IAsyncLifetime
{
    readonly RedisContainer redisContainer = new RedisBuilder("redis:7-alpine").Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await redisContainer.StartAsync();
        ConnectionString = redisContainer.GetConnectionString();
    }

    public async Task DisposeAsync() => await redisContainer.DisposeAsync();
}
