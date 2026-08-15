using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using team_hub_auth.Dtos;
using team_hub_auth.Models;
using team_hub_auth.Services.Tokens;

namespace team_hub_auth.Tests.Controllers.Auth;

public class AuthControllerResponseMappingTests
{
    [Fact]
    public async Task Login_WhenAccessTokenAlreadyExpired_ShouldNotReturnNegativeExpiresInSeconds()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Identity = new UserIdentity
            {
                Username = "john",
                Email = "john@example.com"
            },
            Profile = new UserProfile
            {
                Name = "John",
                Surname = "Doe"
            },
            Credentials = new UserCredentials
            {
                PasswordHash = AuthControllerTestHelpers.PasswordHasher.Hash("secret123")
            }
        });
        await db.SaveChangesAsync();

        var controller = AuthControllerTestHelpers.CreateController(db, tokenService: new ExpiredTokenService());
        var result = await controller.Login(new LoginRequest
        {
            Username = "john",
            Password = "secret123"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal(0, response.ExpiresInSeconds);
        Assert.Equal("john", response.User.Username);
        Assert.Null(response.User.AvatarUrl);
    }

    sealed class ExpiredTokenService : ITokenService
    {
        public (string token, DateTimeOffset expiresAt) GenerateAccessToken(User user) =>
            ("access-token", DateTimeOffset.UtcNow.AddSeconds(-15));

        public (string token, string hash, DateTimeOffset expiresAt) GenerateRefreshToken() =>
            ("refresh-token", "refresh-hash", DateTimeOffset.UtcNow.AddDays(7));

        public string HashRefreshToken(string refreshToken) => refreshToken;
    }
}
