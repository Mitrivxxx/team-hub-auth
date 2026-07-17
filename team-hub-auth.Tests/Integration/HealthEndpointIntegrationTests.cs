using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using team_hub_auth.Configuration;
using Xunit;

namespace team_hub_auth.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class HealthEndpointIntegrationTests(HealthIntegrationFixture fixture) : IClassFixture<HealthIntegrationFixture>
{
    [Fact]
    public async Task Health_WhenPostgresAndRedisAreAvailable_ShouldReturnOk()
    {
        await using var factory = new TestAuthWebApplicationFactory(
            fixture.PostgresConnectionString,
            fixture.RedisConnectionString);

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

[Trait("Category", "Integration")]
public sealed class RedisHealthCheckTests(RedisIntegrationFixture fixture) : IClassFixture<RedisIntegrationFixture>
{
    [Fact]
    public async Task RedisHealthCheck_WhenRedisIsAvailable_ShouldReturnHealthy()
    {
        await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        var healthCheck = new RedisHealthCheck(multiplexer, Microsoft.Extensions.Logging.Abstractions.NullLogger<RedisHealthCheck>.Instance);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }
}
