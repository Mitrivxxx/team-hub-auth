using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using team_hub_auth.Models;
using team_hub_auth.Services.Sessions;

namespace team_hub_auth.Tests.Controllers;

public class AuthControllerLogoutTests
{
    [Fact]
    public async Task Logout_WhenCookieIsMissing_ShouldReturnNoContent()
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

        var result = await controller.Logout();

        var noContent = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContent.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenCookieMatchesSession_ShouldReturnNoContentRevokeSessionAndDeleteCookie()
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
        await sessionStore.StoreRefreshSessionAsync(refreshTokenHash, user.Id, rememberMe: false, refreshTokenExpiresAt);

        var controller = AuthControllerTestHelpers.CreateController(
            db,
            requestCookie: $"refreshToken={refreshToken}",
            tokenService,
            sessionStore);

        var result = await controller.Logout();

        var noContent = Assert.IsType<NoContentResult>(result);
        var setCookieHeader = controller.Response.Headers.SetCookie.ToString();
        var session = await sessionStore.GetRefreshSessionAsync(refreshTokenHash);

        Assert.Equal(StatusCodes.Status204NoContent, noContent.StatusCode);
        Assert.Null(session);
        Assert.Contains("refreshToken=", setCookieHeader, StringComparison.Ordinal);
        Assert.Contains("expires=", setCookieHeader, StringComparison.OrdinalIgnoreCase);
    }
}
