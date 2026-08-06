using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using team_hub_auth.Dtos;
using team_hub_auth.Models;
using team_hub_auth.Services.Sessions;

namespace team_hub_auth.Tests.Controllers;

public class AuthControllerRefreshTests
{
    [Fact]
    public async Task Refresh_WhenCookieIsMissing_ShouldReturnUnauthorized()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.Refresh();

        var unauthorized = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        Assert.IsType<ProblemDetails>(unauthorized.Value);
    }

    [Fact]
    public async Task Refresh_WhenTokenIsInvalid_ShouldReturnUnauthorizedAndDeleteCookie()
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

        var controller = AuthControllerTestHelpers.CreateController(db, requestCookie: "refreshToken=invalid-token");

        var result = await controller.Refresh();

        var setCookieHeader = controller.Response.Headers.SetCookie.ToString();
        var unauthorized = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        Assert.IsType<ProblemDetails>(unauthorized.Value);
        Assert.Contains("refreshToken=", setCookieHeader, StringComparison.Ordinal);
        Assert.Contains("expires=", setCookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_WhenTokenIsValid_ShouldReturnOkRotateCookieAndRefreshToken()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var sessionStore = new InMemorySessionStore();
        var tokenService = AuthControllerTestHelpers.CreateTokenService(expireMinutes: 15);
        var (refreshToken, refreshTokenHash, refreshTokenExpiresAt) = tokenService.GenerateRefreshToken();

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
        await sessionStore.StoreRefreshSessionAsync(refreshTokenHash, user.Id, rememberMe: true, refreshTokenExpiresAt);

        var controller = AuthControllerTestHelpers.CreateController(
            db,
            requestCookie: $"refreshToken={refreshToken}; refreshTokenPersistent=1",
            tokenService,
            sessionStore);

        var result = await controller.Refresh();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        var setCookieHeader = controller.Response.Headers.SetCookie.ToString();
        var newRefreshToken = AuthControllerTestHelpers.GetSetCookieValue(controller.Response.Headers, "refreshToken");
        Assert.False(string.IsNullOrWhiteSpace(newRefreshToken));
        var oldSession = await sessionStore.GetRefreshSessionAsync(refreshTokenHash);
        var newSession = await sessionStore.GetRefreshSessionAsync(tokenService.HashRefreshToken(newRefreshToken!));

        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.Contains("refreshToken=", setCookieHeader, StringComparison.Ordinal);
        Assert.Contains("refreshTokenPersistent=1", setCookieHeader, StringComparison.Ordinal);
        Assert.Contains("expires=", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Null(oldSession);
        Assert.NotNull(newSession);
        Assert.Equal(user.Id, newSession.UserId);
        Assert.True(newSession.RememberMe);
    }
}
