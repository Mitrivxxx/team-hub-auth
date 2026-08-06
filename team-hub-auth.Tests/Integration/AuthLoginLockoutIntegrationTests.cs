using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using team_hub_auth.Dtos;
using team_hub_auth.Data;
using team_hub_auth.Models;
using team_hub_auth.Tests.Controllers;
using TeamHub.Observability;

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
            var response = await client.PostAsJsonAsync("/api/auth/v0.0/login", new LoginRequest
            {
                Username = username,
                Password = "wrong-password",
                RememberMe = false
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);
            var body = document.RootElement;

            Assert.Equal(ProblemTypes.For("invalid-credentials"), body.GetProperty("type").GetString());
            Assert.Equal("AUTH_INVALID_CREDENTIALS", body.GetProperty("code").GetString());
            Assert.Equal(MaxFailedLoginAttempts - i, body.GetProperty("remainingAttempts").GetInt32());
            Assert.True(
                !body.TryGetProperty("lockoutSeconds", out var lockout) ||
                lockout.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined);
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

        for (var i = 1; i <= 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/v0.0/login", new LoginRequest
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
                Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType?.MediaType);

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(stream);
                var body = document.RootElement;

                Assert.Equal(ProblemTypes.For("account-locked"), body.GetProperty("type").GetString());
                Assert.Equal("AUTH_LOCKED", body.GetProperty("code").GetString());
                Assert.Equal(0, body.GetProperty("remainingAttempts").GetInt32());
                Assert.True(body.TryGetProperty("lockoutSeconds", out var lockoutSeconds));
                Assert.Equal(JsonValueKind.Number, lockoutSeconds.ValueKind);
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

        Assert.InRange((int)ttl.Value.TotalSeconds, 0, (int)Math.Ceiling(LockoutDuration.TotalSeconds));
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

        for (var i = 1; i <= 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/v0.0/login", new LoginRequest
            {
                Username = username,
                Password = "wrong-password",
                RememberMe = false
            });
            Assert.NotNull(response);
        }

        var lockedResponse = await client.PostAsJsonAsync("/api/auth/v0.0/login", new LoginRequest
        {
            Username = username,
            Password = "secret123456",
            RememberMe = false
        });

        Assert.Equal((HttpStatusCode)423, lockedResponse.StatusCode);

        await ClearRedisKeysAsync(factory, username);

        var okResponse = await client.PostAsJsonAsync("/api/auth/v0.0/login", new LoginRequest
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

        db.Users.RemoveRange(db.Users.Where(u => u.Identity.Username.ToLower() == username.ToLowerInvariant()));
        await db.SaveChangesAsync();

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Identity = new UserIdentity
            {
                Username = username,
                Email = ""
            },
            Profile = new UserProfile
            {
                Name = "John",
                Surname = "Doe"
            },
            Credentials = new UserCredentials
            {
                PasswordHash = AuthControllerTestHelpers.PasswordHasher.Hash(password)
            }
        });

        await db.SaveChangesAsync();
    }

    static async Task ClearRedisKeysAsync(TestAuthWebApplicationFactory factory, string username)
    {
        var usernameLower = username.ToLowerInvariant();
        var attemptsKey = $"auth:login-attempts:{usernameLower}";
        var lockoutKey = $"auth:login-lockout:{usernameLower}";

        using var scope = factory.Services.CreateScope();
        var multiplexer = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var db = multiplexer.GetDatabase();

        await db.KeyDeleteAsync(attemptsKey);
        await db.KeyDeleteAsync(lockoutKey);
    }
}
