using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using team_hub_auth.Configuration;
using team_hub_auth.Services.Sessions;
using Xunit;

namespace team_hub_auth.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class RedisSessionStoreIntegrationTests(RedisIntegrationFixture fixture) : IClassFixture<RedisIntegrationFixture>
{
    [Fact]
    public void AddRedisSessionStore_ShouldBindConfigurationAndResolveRedisDependencies()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RedisOptions.SectionName}:ConnectionString"] = fixture.ConnectionString
            })
            .Build();

        services.AddRedisSessionStore(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        var redisOptions = provider.GetRequiredService<IOptions<RedisOptions>>().Value;
        var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
        var sessionStore = provider.GetRequiredService<ISessionStore>();

        Assert.Equal(fixture.ConnectionString, redisOptions.ConnectionString);
        Assert.IsType<RedisSessionStore>(sessionStore);
        Assert.True(multiplexer.IsConnected);
    }

    [Fact]
    public async Task RedisSessionStore_ShouldStoreGetAndRevokeSession()
    {
        await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        var sessionStore = new RedisSessionStore(multiplexer, NullLogger<RedisSessionStore>.Instance);

        var userId = Guid.NewGuid();
        const string refreshTokenHash = "integration-test-hash";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);

        await sessionStore.StoreRefreshSessionAsync(refreshTokenHash, userId, rememberMe: true, expiresAt);

        var storedSession = await sessionStore.GetRefreshSessionAsync(refreshTokenHash);
        Assert.NotNull(storedSession);
        Assert.Equal(userId, storedSession.UserId);
        Assert.True(storedSession.RememberMe);

        await sessionStore.RevokeRefreshSessionAsync(refreshTokenHash);

        var revokedSession = await sessionStore.GetRefreshSessionAsync(refreshTokenHash);
        Assert.Null(revokedSession);
    }
}
