using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using team_hub_auth.Models;

namespace team_hub_auth.Tests.Controllers;

public class AuthControllerLogoutTests
{
    [Fact]
    public async Task Logout_WhenCookieIsMissing_ShouldReturnNoContentAndNotTouchRefreshFields()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var role = await AuthControllerTestHelpers.EnsureRoleAsync(db, "user");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "john",
            Name = "John",
            Surname = "Doe",
            Password = AuthControllerTestHelpers.PasswordHasher.Hash("secret123"),
            RoleId = role.Id,
            RefreshTokenHash = "HASH_VALUE",
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.Logout();

        var noContent = Assert.IsType<NoContentResult>(result);
        var unchangedUser = await db.Users.SingleAsync(u => u.Id == user.Id);

        Assert.Equal(StatusCodes.Status204NoContent, noContent.StatusCode);
        Assert.Equal("HASH_VALUE", unchangedUser.RefreshTokenHash);
        Assert.NotNull(unchangedUser.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task Logout_WhenCookieMatchesUser_ShouldReturnNoContentClearRefreshAndDeleteCookie()
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

        var result = await controller.Logout();

        var noContent = Assert.IsType<NoContentResult>(result);
        var setCookieHeader = controller.Response.Headers.SetCookie.ToString();
        var updatedUser = await db.Users.SingleAsync(u => u.Id == user.Id);

        Assert.Equal(StatusCodes.Status204NoContent, noContent.StatusCode);
        Assert.Null(updatedUser.RefreshTokenHash);
        Assert.Null(updatedUser.RefreshTokenExpiresAt);
        Assert.Contains("refreshToken=", setCookieHeader, StringComparison.Ordinal);
        Assert.Contains("expires=", setCookieHeader, StringComparison.OrdinalIgnoreCase);
    }
}
