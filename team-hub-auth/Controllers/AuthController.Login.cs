using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using team_hub_auth.Dtos;
using team_hub_auth.Models;

namespace team_hub_auth.Controllers;

public partial class AuthController
{
    /// <summary>Sign in and issue auth tokens.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var username = (req.Username ?? string.Empty).Trim();
        logger.LogInformation("Login attempt for username {Username}", username);

        var now = DateTimeOffset.UtcNow;
        var loweredUsername = username.ToLower();
        var user = await db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == loweredUsername);

        if (user?.LockoutUntil is { } lockoutUntil && lockoutUntil > now)
        {
            logger.LogWarning("Login blocked for username {Username} due to active lockout", username);
            return Unauthorized();
        }

        if (user is null || !passwordHasher.Verify(req.Password, user.Password))
        {
            if (user is not null)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
                {
                    user.LockoutUntil = now.Add(LockoutDuration);
                    user.FailedLoginAttempts = 0;
                }

                await db.SaveChangesAsync();
            }

            logger.LogWarning("Login failed for username {Username}", username);
            return Unauthorized();
        }

        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        await db.SaveChangesAsync();

        var roleName = user.Role?.Name;
        var (accessToken, accessTokenExpiresAt) = tokenService.GenerateAccessToken(user, roleName);
        var (refreshToken, refreshTokenHash, refreshTokenExpiresAt) = tokenService.GenerateRefreshToken();

        await sessionStore.StoreRefreshSessionAsync(
            refreshTokenHash,
            user.Id,
            req.RememberMe,
            refreshTokenExpiresAt);

        SetRefreshTokenCookie(refreshToken, refreshTokenExpiresAt, req.RememberMe);

        logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return Ok(ToAuthResponse(user, roleName, accessToken, accessTokenExpiresAt));
    }
}
