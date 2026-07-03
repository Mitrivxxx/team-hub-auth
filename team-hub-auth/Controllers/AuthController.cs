using Medo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using team_hub_auth.Data;
using team_hub_auth.Dtos;
using team_hub_auth.Models;
using team_hub_auth.Services;

namespace team_hub_auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthDbContext db, TokenService tokenService, ILogger<AuthController> logger) : ControllerBase
{
    const string RefreshTokenCookieName = "refreshToken";

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        logger.LogInformation("Registration attempt for username {Username}", req.Username);

        if (await db.Users.AnyAsync(u => u.Username == req.Username))
        {
            logger.LogWarning("Registration failed: username {Username} already exists", req.Username);
            return Conflict("Username already exists.");
        }

        var user = new User
        {
            Id = Uuid7.NewGuid(),
            Username = req.Username,
            Name = req.Name,
            Surname = req.Surname,
            Password = PasswordHasher.Hash(req.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        logger.LogInformation("User {UserId} registered successfully with username {Username}", user.Id, user.Username);

        return Created($"/api/users/{user.Id}", ToResponse(user, null));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        logger.LogInformation("Login attempt for username {Username}", req.Username);

        var user = await db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == req.Username);

        if (user is null || !PasswordHasher.Verify(req.Password, user.Password))
        {
            logger.LogWarning("Login failed for username {Username}", req.Username);
            return Unauthorized();
        }

        var roleName = user.Role?.Name;
        var (accessToken, accessTokenExpiresAt) = tokenService.GenerateAccessToken(user, roleName);
        var (refreshToken, refreshTokenHash, refreshTokenExpiresAt) = tokenService.GenerateRefreshToken();

        user.RefreshTokenHash = refreshTokenHash;
        user.RefreshTokenExpiresAt = refreshTokenExpiresAt;
        await db.SaveChangesAsync();

        SetRefreshTokenCookie(refreshToken, refreshTokenExpiresAt);

        logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return Ok(ToAuthResponse(user, roleName, accessToken, accessTokenExpiresAt));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized();
        }

        var refreshTokenHash = tokenService.HashRefreshToken(refreshToken);
        var now = DateTimeOffset.UtcNow;

        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.RefreshTokenHash == refreshTokenHash &&
                u.RefreshTokenExpiresAt != null &&
                u.RefreshTokenExpiresAt > now);

        if (user is null)
        {
            DeleteRefreshTokenCookie();
            return Unauthorized();
        }

        var roleName = user.Role?.Name;
        var (accessToken, accessTokenExpiresAt) = tokenService.GenerateAccessToken(user, roleName);
        var (newRefreshToken, newRefreshTokenHash, newRefreshTokenExpiresAt) = tokenService.GenerateRefreshToken();

        user.RefreshTokenHash = newRefreshTokenHash;
        user.RefreshTokenExpiresAt = newRefreshTokenExpiresAt;
        await db.SaveChangesAsync();

        SetRefreshTokenCookie(newRefreshToken, newRefreshTokenExpiresAt);

        return Ok(ToAuthResponse(user, roleName, accessToken, accessTokenExpiresAt));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var refreshTokenHash = tokenService.HashRefreshToken(refreshToken);
            var user = await db.Users.FirstOrDefaultAsync(u => u.RefreshTokenHash == refreshTokenHash);
            if (user is not null)
            {
                user.RefreshTokenHash = null;
                user.RefreshTokenExpiresAt = null;
                await db.SaveChangesAsync();
            }
        }

        DeleteRefreshTokenCookie();
        return NoContent();
    }

    static UserResponse ToResponse(User user, string? role) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Name = user.Name,
        Surname = user.Surname,
        Role = role
    };

    static AuthResponse ToAuthResponse(User user, string? role, string accessToken, DateTimeOffset accessTokenExpiresAt) => new()
    {
        AccessToken = accessToken,
        ExpiresInSeconds = (int)Math.Max(0, (accessTokenExpiresAt - DateTimeOffset.UtcNow).TotalSeconds),
        User = ToResponse(user, role)
    };

    void SetRefreshTokenCookie(string token, DateTimeOffset expiresAt)
    {
        Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt
        });
    }

    void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Strict
        });
    }
}
