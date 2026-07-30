using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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

        var loweredUsername = username.ToLower();

        var lockoutStatus = await loginAttemptLimiter.GetLockoutStatusAsync(loweredUsername);
        if (lockoutStatus.IsLocked)
        {
            logger.LogWarning("Login blocked for username {Username} due to active lockout", username);
            return StatusCode(StatusCodes.Status423Locked, new AuthLoginErrorResponse
            {
                Code = "AUTH_LOCKED",
                RemainingAttempts = 0,
                LockoutSeconds = lockoutStatus.LockoutSeconds
            });
        }

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Identity.Username.ToLower() == loweredUsername);

        var passwordIsValid = user is not null && passwordHasher.Verify(req.Password, user.Credentials.PasswordHash);
        if (!passwordIsValid)
        {
            var failureOutcome = await loginAttemptLimiter.RegisterFailedAttemptAsync(
                loweredUsername,
                MaxFailedLoginAttempts,
                LockoutDuration);

            logger.LogWarning("Login failed for username {Username} (remainingAttempts: {RemainingAttempts})", username, failureOutcome.RemainingAttempts);

            if (failureOutcome.IsLocked)
            {
                return StatusCode(StatusCodes.Status423Locked, new AuthLoginErrorResponse
                {
                    Code = "AUTH_LOCKED",
                    RemainingAttempts = 0,
                    LockoutSeconds = failureOutcome.LockoutSeconds
                });
            }

            return Unauthorized(new AuthLoginErrorResponse
            {
                Code = "AUTH_INVALID_CREDENTIALS",
                RemainingAttempts = failureOutcome.RemainingAttempts,
                LockoutSeconds = null
            });
        }

        await loginAttemptLimiter.ClearAttemptsAsync(loweredUsername);

        var (accessToken, accessTokenExpiresAt) = tokenService.GenerateAccessToken(user);
        var (refreshToken, refreshTokenHash, refreshTokenExpiresAt) = tokenService.GenerateRefreshToken();

        await sessionStore.StoreRefreshSessionAsync(
            refreshTokenHash,
            user!.Id,
            req.RememberMe,
            refreshTokenExpiresAt);

        SetRefreshTokenCookie(refreshToken, refreshTokenExpiresAt, req.RememberMe);

        logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return Ok(ToAuthResponse(user!, accessToken, accessTokenExpiresAt));
    }
}
