using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace team_hub_auth.Tests.Integration;

public sealed class HealthIntegrationFixture : IAsyncLifetime
{
    readonly RedisContainer redisContainer = new RedisBuilder("redis:7-alpine").Build();
    readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("auth")
        .WithUsername("matsma")
        .WithPassword("test")
        .Build();

    public string RedisConnectionString { get; private set; } = string.Empty;

    public string PostgresConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(redisContainer.StartAsync(), postgresContainer.StartAsync());
        RedisConnectionString = redisContainer.GetConnectionString();
        PostgresConnectionString = postgresContainer.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        await redisContainer.DisposeAsync();
        await postgresContainer.DisposeAsync();
    }
}
