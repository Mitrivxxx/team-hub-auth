using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using team_hub_auth.Controllers;
using team_hub_auth.Data;
using team_hub_auth.Dtos;
using team_hub_auth.Models;
using team_hub_auth.Services.Password;
using team_hub_auth.Services.Sessions;
using team_hub_auth.Services.Tokens;

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
            Username = "alice",
            Name = "Alice",
            Surname = "Smith",
            Password = "old-hash",
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
        Assert.Equal("hashed:NewPassword123!", updatedUser.Password);
        Assert.Equal(0, updatedUser.FailedLoginAttempts);
        Assert.Null(updatedUser.LockoutUntil);
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
            Username = "alice",
            Name = "Alice",
            Surname = "Smith",
            Password = "old-hash",
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

        Assert.IsType<UnauthorizedResult>(result);
    }

    static AuthController CreateController(AuthDbContext db, IPasswordHasher passwordHasher) =>
        new(
            db,
            new TestTokenService(),
            new TestSessionStore(),
            passwordHasher,
            NullLogger<AuthController>.Instance);

    sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
    }

    sealed class TestTokenService : ITokenService
    {
        public (string token, DateTimeOffset expiresAt) GenerateAccessToken(User user, string? roleName) =>
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
