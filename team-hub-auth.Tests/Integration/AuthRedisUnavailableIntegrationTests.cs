using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using team_hub_auth.Data;
using team_hub_auth.Dtos;
using team_hub_auth.Models;
using team_hub_auth.Services.Sessions;
using team_hub_auth.Tests.Controllers.Auth;
using Xunit;

namespace team_hub_auth.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class AuthRedisUnavailableIntegrationTests(HealthIntegrationFixture fixture) : IClassFixture<HealthIntegrationFixture>
{
    [Fact]
    public async Task Login_WhenRedisIsUnavailable_ShouldReturnServiceUnavailable()
    {
        await using var factory = new TestAuthWebApplicationFactory(
            fixture.PostgresConnectionString,
            fixture.RedisConnectionString,
            services => services.AddSingleton<ISessionStore, RedisUnavailableSessionStore>());

        await SeedUserAsync(factory);

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/v1/login", new LoginRequest
        {
            Username = "john",
            Password = "secret123456",
            RememberMe = false
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "Authentication service temporarily unavailable. Please try again later.",
            body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Refresh_WhenRedisIsUnavailable_ShouldReturnServiceUnavailable()
    {
        await using var factory = new TestAuthWebApplicationFactory(
            fixture.PostgresConnectionString,
            fixture.RedisConnectionString,
            services => services.AddSingleton<ISessionStore, RedisUnavailableSessionStore>());

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "refreshToken=test-token");

        var response = await client.PostAsync("/api/auth/v1/refresh", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "Authentication service temporarily unavailable. Please try again later.",
            body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Logout_WhenRedisIsUnavailable_ShouldReturnServiceUnavailable()
    {
        await using var factory = new TestAuthWebApplicationFactory(
            fixture.PostgresConnectionString,
            fixture.RedisConnectionString,
            services => services.AddSingleton<ISessionStore, RedisUnavailableSessionStore>());

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "refreshToken=test-token");

        var response = await client.PostAsync("/api/auth/v1/logout", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var logoutBody = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "Authentication service temporarily unavailable. Please try again later.",
            logoutBody.GetProperty("detail").GetString());
    }

    static async Task SeedUserAsync(TestAuthWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Identity = new UserIdentity
            {
                Username = "john",
                Email = ""
            },
            Profile = new UserProfile
            {
                Name = "John",
                Surname = "Doe"
            },
            Credentials = new UserCredentials
            {
                PasswordHash = AuthControllerTestHelpers.PasswordHasher.Hash("secret123456")
            }
        });
        await db.SaveChangesAsync();
    }
}
