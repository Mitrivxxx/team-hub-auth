using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using team_hub_auth.Dtos;
using team_hub_auth.Models;

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
            RoleId = role.Id,
            RefreshTokenHash = "UNRELATED_HASH",
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
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
            RoleId = role.Id,
            RefreshTokenHash = refreshTokenHash,
            RefreshTokenExpiresAt = refreshTokenExpiresAt
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = AuthControllerTestHelpers.CreateController(db, requestCookie: $"refreshToken={refreshToken}", tokenService);

        var result = await controller.Refresh();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        var setCookieHeader = controller.Response.Headers.SetCookie.ToString();
        var updatedUser = await db.Users.SingleAsync(u => u.Id == user.Id);

        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.Contains("refreshToken=", setCookieHeader, StringComparison.Ordinal);
        Assert.NotNull(updatedUser.RefreshTokenHash);
        Assert.NotEqual(refreshTokenHash, updatedUser.RefreshTokenHash);
    }
}
