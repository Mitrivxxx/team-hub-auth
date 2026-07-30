using Microsoft.AspNetCore.Mvc;
using team_hub_auth.Dtos;
using team_hub_auth.Models;
using team_hub_auth.Services.Sessions;

namespace team_hub_auth.Tests.Controllers;

public class AuthControllerLoginTests
{
    [Fact]
    public async Task Login_WhenUserDoesNotExist_ShouldReturnUnauthorized()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.Login(new LoginRequest
        {
            Username = "unknown",
            Password = "secret123"
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<AuthLoginErrorResponse>(unauthorized.Value);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", response.Code);
        Assert.True(response.RemainingAttempts is not null);
    }

    [Fact]
    public async Task Login_WhenPasswordIsInvalid_ShouldReturnUnauthorized()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
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
                PasswordHash = AuthControllerTestHelpers.PasswordHasher.Hash("secret123")
            }
        });
        await db.SaveChangesAsync();

        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.Login(new LoginRequest
        {
            Username = "john",
            Password = "wrong-password"
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<AuthLoginErrorResponse>(unauthorized.Value);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", response.Code);
        Assert.True(response.RemainingAttempts is not null);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValidAndRememberMeDisabled_ShouldReturnOkWithSessionCookie()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var sessionStore = new InMemorySessionStore();
        var user = new User
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
                PasswordHash = AuthControllerTestHelpers.PasswordHasher.Hash("secret123")
            }
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var tokenService = AuthControllerTestHelpers.CreateTokenService(expireMinutes: 15);
        var controller = AuthControllerTestHelpers.CreateController(db, sessionStore: sessionStore, tokenService: tokenService);

        var result = await controller.Login(new LoginRequest
        {
            Username = "john",
            Password = "secret123",
            RememberMe = false
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        var setCookieHeader = controller.Response.Headers.SetCookie.ToString();
        var refreshToken = AuthControllerTestHelpers.GetSetCookieValue(controller.Response.Headers, "refreshToken");
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));
        var refreshTokenHash = tokenService.HashRefreshToken(refreshToken!);
        var session = await sessionStore.GetRefreshSessionAsync(refreshTokenHash);

        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.Contains("refreshToken=", setCookieHeader, StringComparison.Ordinal);
        Assert.Contains("refreshTokenPersistent=0", setCookieHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("expires=", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(session);
        Assert.Equal(user.Id, session.UserId);
        Assert.False(session.RememberMe);
    }

    [Fact]
    public async Task Login_WhenRememberMeEnabled_ShouldReturnOkWithPersistentCookie()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var user = new User
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
                PasswordHash = AuthControllerTestHelpers.PasswordHasher.Hash("secret123")
            }
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.Login(new LoginRequest
        {
            Username = "john",
            Password = "secret123",
            RememberMe = true
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<AuthResponse>(ok.Value);
        var setCookieHeader = controller.Response.Headers.SetCookie.ToString();

        Assert.Contains("refreshToken=", setCookieHeader, StringComparison.Ordinal);
        Assert.Contains("refreshTokenPersistent=1", setCookieHeader, StringComparison.Ordinal);
        Assert.Contains("expires=", setCookieHeader, StringComparison.OrdinalIgnoreCase);
    }
}
