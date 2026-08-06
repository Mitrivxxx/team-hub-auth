using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using team_hub_auth.Controllers;
using team_hub_auth.Data;
using team_hub_auth.Dtos;
using team_hub_auth.Models;
using team_hub_auth.Services.LoginAttempts;
using team_hub_auth.Services.Password;
using team_hub_auth.Services.Sessions;
using team_hub_auth.Services.Tokens;
using team_hub_auth.Services.Users;

namespace team_hub_auth.Tests.Controllers;

public class AuthControllerChangePasswordTests
{
    [Fact]
    public async Task ChangePassword_ValidIdentity_ReturnsNoContentAndUpdatesPassword()
    {
        var userId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(nameof(ChangePassword_ValidIdentity_ReturnsNoContentAndUpdatesPassword))
            .Options;

        await using var db = new AuthDbContext(options);
        db.Users.Add(new User
        {
            Id = userId,
            Identity = new UserIdentity
            {
                Username = "alice",
                Email = ""
            },
            Profile = new UserProfile
            {
                Name = "Alice",
                Surname = "Smith"
            },
            Credentials = new UserCredentials
            {
                PasswordHash = "old-hash"
            }
        });
        await db.SaveChangesAsync();

        var passwordHasher = new TestPasswordHasher();
        var controller = CreateController(db, passwordHasher);

        var result = await controller.ChangePassword(new ChangePasswordRequest
        {
            Username = "alice",
            Name = "Alice",
            Surname = "Smith",
            Password = "NewPassword123!",
        });

        Assert.IsType<NoContentResult>(result);

        var updatedUser = await db.Users.SingleAsync(u => u.Id == userId);
        Assert.Equal("hashed:NewPassword123!", updatedUser.Credentials.PasswordHash);
        Assert.Equal(0, updatedUser.Security.FailedLoginAttempts);
        Assert.Null(updatedUser.Security.LockoutUntil);
    }

    [Fact]
    public async Task ChangePassword_InvalidIdentity_ReturnsUnauthorized()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(nameof(ChangePassword_InvalidIdentity_ReturnsUnauthorized))
            .Options;

        await using var db = new AuthDbContext(options);
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Identity = new UserIdentity
            {
                Username = "alice",
                Email = ""
            },
            Profile = new UserProfile
            {
                Name = "Alice",
                Surname = "Smith"
            },
            Credentials = new UserCredentials
            {
                PasswordHash = "old-hash"
            }
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, new TestPasswordHasher());

        var result = await controller.ChangePassword(new ChangePasswordRequest
        {
            Username = "alice",
            Name = "Alice",
            Surname = "Wrong",
            Password = "NewPassword123!",
        });

        var unauthorized = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    static AuthController CreateController(AuthDbContext db, IPasswordHasher passwordHasher) =>
        new(
            db,
            new TestTokenService(),
            new TestSessionStore(),
            passwordHasher,
            new InMemoryLoginAttemptLimiter(),
            new UserQueryService(db),
            NullLogger<AuthController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

    sealed class InMemoryLoginAttemptLimiter : ILoginAttemptLimiter
    {
        readonly Dictionary<string, int> attemptsByUsername = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, DateTimeOffset> lockoutUntilByUsername = new(StringComparer.OrdinalIgnoreCase);

        public Task<LoginLockoutStatus> GetLockoutStatusAsync(string username, CancellationToken cancellationToken = default)
        {
            if (lockoutUntilByUsername.TryGetValue(username, out var lockoutUntil) && lockoutUntil > DateTimeOffset.UtcNow)
            {
                var remainingSeconds = (int)Math.Ceiling((lockoutUntil - DateTimeOffset.UtcNow).TotalSeconds);
                return Task.FromResult(new LoginLockoutStatus(IsLocked: true, LockoutSeconds: Math.Max(0, remainingSeconds)));
            }

            return Task.FromResult(new LoginLockoutStatus(IsLocked: false, LockoutSeconds: 0));
        }

        public Task<LoginFailureOutcome> RegisterFailedAttemptAsync(
            string username,
            int maxFailedAttempts,
            TimeSpan lockoutDuration,
            CancellationToken cancellationToken = default)
        {
            attemptsByUsername.TryGetValue(username, out var attempts);
            attempts++;

            if (attempts >= maxFailedAttempts)
            {
                lockoutUntilByUsername[username] = DateTimeOffset.UtcNow.Add(lockoutDuration);
                attemptsByUsername[username] = 0;
                return Task.FromResult(new LoginFailureOutcome(RemainingAttempts: 0, IsLocked: true, LockoutSeconds: (int)Math.Ceiling(lockoutDuration.TotalSeconds)));
            }

            attemptsByUsername[username] = attempts;
            var remainingAttempts = Math.Max(0, maxFailedAttempts - attempts);
            return Task.FromResult(new LoginFailureOutcome(RemainingAttempts: remainingAttempts, IsLocked: false, LockoutSeconds: 0));
        }

        public Task ClearAttemptsAsync(string username, CancellationToken cancellationToken = default)
        {
            attemptsByUsername.Remove(username);
            lockoutUntilByUsername.Remove(username);
            return Task.CompletedTask;
        }
    }

    sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
    }

    sealed class TestTokenService : ITokenService
    {
        public (string token, DateTimeOffset expiresAt) GenerateAccessToken(User user) =>
            ("access-token", DateTimeOffset.UtcNow.AddMinutes(15));

        public (string token, string hash, DateTimeOffset expiresAt) GenerateRefreshToken() =>
            ("refresh-token", "refresh-hash", DateTimeOffset.UtcNow.AddDays(7));

        public string HashRefreshToken(string refreshToken) => refreshToken;
    }

    sealed class TestSessionStore : ISessionStore
    {
        public Task StoreRefreshSessionAsync(
            string refreshTokenHash,
            Guid userId,
            bool rememberMe,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<RefreshSession?> GetRefreshSessionAsync(
            string refreshTokenHash,
            CancellationToken cancellationToken = default) => Task.FromResult<RefreshSession?>(null);

        public Task RevokeRefreshSessionAsync(
            string refreshTokenHash,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
