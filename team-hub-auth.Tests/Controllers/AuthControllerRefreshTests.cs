using Microsoft.AspNetCore.Mvc;
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

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Refresh_WhenTokenIsInvalid_ShouldReturnUnauthorizedAndDeleteCookie()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var role = await AuthControllerTestHelpers.EnsureRoleAsync(db, "user");
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "john",
            Name = "John",
            Surname = "Doe",
            Password = AuthControllerTestHelpers.PasswordHasher.Hash("secret123"),
            RoleId = role.Id
        });
        await db.SaveChangesAsync();

        var controller = AuthControllerTestHelpers.CreateController(db, requestCookie: "refreshToken=invalid-token");

        var result = await controller.Refresh();

        var setCookieHeader = controller.Response.Headers.SetCookie.ToString();
        Assert.IsType<UnauthorizedResult>(result);
        Assert.Contains("refreshToken=", setCookieHeader, StringComparison.Ordinal);
        Assert.Contains("expires=", setCookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_WhenTokenIsValid_ShouldReturnOkRotateCookieAndRefreshToken()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var sessionStore = new InMemorySessionStore();
        var role = await AuthControllerTestHelpers.EnsureRoleAsync(db, "user");
        var tokenService = AuthControllerTestHelpers.CreateTokenService(expireMinutes: 15);
        var (refreshToken, refreshTokenHash, refreshTokenExpiresAt) = tokenService.GenerateRefreshToken();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "john",
            Name = "John",
            Surname = "Doe",
            Password = AuthControllerTestHelpers.PasswordHasher.Hash("secret123"),
            RoleId = role.Id
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
