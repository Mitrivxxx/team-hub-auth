using System.Net;
using System.Net.Http.Json;
using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;
using Xunit;
using team_hub_auth.Dtos;
using team_hub_auth.Data;
using team_hub_auth.Models;
using team_hub_auth.Tests.Controllers;

namespace team_hub_auth.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class AuthLoginLockoutIntegrationTests(HealthIntegrationFixture fixture) : IClassFixture<HealthIntegrationFixture>
{
    const int MaxFailedLoginAttempts = 5;
    static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    [Fact]
    public async Task Login_FailedAttempts_1Through4_Return401WithDecreasingRemainingAttempts()
    {
        await using var factory = new TestAuthWebApplicationFactory(
            fixture.PostgresConnectionString,
            fixture.RedisConnectionString);

        const string username = "john";
        await SeedUserAsync(factory, username, password: "secret123456");
        await ClearRedisKeysAsync(factory, username);

        using var client = factory.CreateClient();

        for (var i = 1; i <= 4; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Username = username,
                Password = "wrong-password",
                RememberMe = false
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<AuthLoginErrorResponse>();
            Assert.NotNull(body);
            Assert.Equal("AUTH_INVALID_CREDENTIALS", body!.Code);

            // After attempt i, remaining = 5 - i
            Assert.Equal(MaxFailedLoginAttempts - i, body.RemainingAttempts);
            Assert.Null(body.LockoutSeconds);
        }
    }

    [Fact]
    public async Task Login_FifthFailedAttempt_Returns423AndSetsLockoutWithTtl()
    {
        await using var factory = new TestAuthWebApplicationFactory(
            fixture.PostgresConnectionString,
            fixture.RedisConnectionString);

        const string username = "john";
        await SeedUserAsync(factory, username, password: "secret123456");
        await ClearRedisKeysAsync(factory, username);

        using var client = factory.CreateClient();

        // 5th failure -> lockout
        for (var i = 1; i <= 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Username = username,
                Password = "wrong-password",
                RememberMe = false
            });

            if (i < MaxFailedLoginAttempts)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
            else
            {
                Assert.Equal((HttpStatusCode)423, response.StatusCode);

                var body = await response.Content.ReadFromJsonAsync<AuthLoginErrorResponse>();
                Assert.NotNull(body);
                Assert.Equal("AUTH_LOCKED", body!.Code);
                Assert.Equal(0, body.RemainingAttempts);
                Assert.NotNull(body.LockoutSeconds);
            }
        }

        await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(fixture.RedisConnectionString);
        var db = multiplexer.GetDatabase();

        var usernameLower = username.ToLowerInvariant();
        var lockoutKey = $"auth:login-lockout:{usernameLower}";
        var attemptsKey = $"auth:login-attempts:{usernameLower}";

        var ttl = await db.KeyTimeToLiveAsync(lockoutKey);
        Assert.NotNull(ttl);
        Assert.True(ttl!.Value > TimeSpan.Zero);

        // Should be ~15 minutes (allow small drift).
        Assert.InRange((int)ttl.Value.TotalSeconds, 0, (int)Math.Ceiling(LockoutDuration.TotalSeconds));

        // Counter should be cleared when lockout is set.
        Assert.False(await db.KeyExistsAsync(attemptsKey));
    }

    [Fact]
    public async Task Login_LockoutBlocksEvenValidCredentialsUntilKeysAreCleared()
    {
        await using var factory = new TestAuthWebApplicationFactory(
            fixture.PostgresConnectionString,
            fixture.RedisConnectionString);

        const string username = "john";
        await SeedUserAsync(factory, username, password: "secret123456");
        await ClearRedisKeysAsync(factory, username);

        using var client = factory.CreateClient();

        // Trigger lockout
        for (var i = 1; i <= 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Username = username,
                Password = "wrong-password",
                RememberMe = false
            });
            Assert.NotNull(response);
        }

        // Now try with correct password: should still be locked
        var lockedResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = username,
            Password = "secret123456",
            RememberMe = false
        });

        Assert.Equal((HttpStatusCode)423, lockedResponse.StatusCode);

        // Clear lockout keys manually and try again.
        await ClearRedisKeysAsync(factory, username);

        var okResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = username,
            Password = "secret123456",
            RememberMe = false
        });

        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
    }

    static async Task SeedUserAsync(TestAuthWebApplicationFactory factory, string username, string password)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var role = await AuthControllerTestHelpers.EnsureRoleAsync(db, "user");

        // Ensure clean user state between tests.
        db.Users.RemoveRange(db.Users.Where(u => u.Username.ToLower() == username.ToLowerInvariant()));
        await db.SaveChangesAsync();

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Name = "John",
            Surname = "Doe",
            Password = AuthControllerTestHelpers.PasswordHasher.Hash(password),
            RoleId = role.Id
        });

        await db.SaveChangesAsync();
    }

    static async Task ClearRedisKeysAsync(TestAuthWebApplicationFactory factory, string username)
    {
        // The limiter uses key segment values:
        // - auth:login-attempts:<username>
        // - auth:login-lockout:<username>
        var usernameLower = username.ToLowerInvariant();
        var attemptsKey = $"auth:login-attempts:{usernameLower}";
        var lockoutKey = $"auth:login-lockout:{usernameLower}";

        using var scope = factory.Services.CreateScope();
        var multiplexer = scope.ServiceProvider.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
        var db = multiplexer.GetDatabase();

        await db.KeyDeleteAsync(attemptsKey);
        await db.KeyDeleteAsync(lockoutKey);
    }
}

