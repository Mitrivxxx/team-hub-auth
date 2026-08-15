using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TeamHub.Observability;
using team_hub_auth.Dtos;
using team_hub_auth.Models;
using team_hub_auth.Observability;

namespace team_hub_auth.Controllers.Auth;

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
            AuthMetrics.RecordLoginLockout();
            logger.LogWarning("Login blocked for username {Username} due to active lockout", username);
            return LoginProblem(
                StatusCodes.Status423Locked,
                ProblemTypes.For("account-locked"),
                "Account locked",
                "Account is locked due to too many failed login attempts.",
                code: "AUTH_LOCKED",
                remainingAttempts: 0,
                lockoutSeconds: lockoutStatus.LockoutSeconds);
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

            AuthMetrics.RecordLoginFailure();
            logger.LogWarning("Login failed for username {Username} (remainingAttempts: {RemainingAttempts})", username, failureOutcome.RemainingAttempts);

            if (failureOutcome.IsLocked)
            {
                AuthMetrics.RecordLoginLockout();
                return LoginProblem(
                    StatusCodes.Status423Locked,
                    ProblemTypes.For("account-locked"),
                    "Account locked",
                    "Account is locked due to too many failed login attempts.",
                    code: "AUTH_LOCKED",
                    remainingAttempts: 0,
                    lockoutSeconds: failureOutcome.LockoutSeconds);
            }

            return LoginProblem(
                StatusCodes.Status401Unauthorized,
                ProblemTypes.For("invalid-credentials"),
                "Unauthorized",
                "Invalid username or password.",
                code: "AUTH_INVALID_CREDENTIALS",
                remainingAttempts: failureOutcome.RemainingAttempts,
                lockoutSeconds: null);
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

    IActionResult LoginProblem(
        int statusCode,
        string type,
        string title,
        string detail,
        string code,
        int? remainingAttempts,
        int? lockoutSeconds)
    {
        var problem = TeamHubProblemDetailsFactory.Create(
            HttpContext,
            statusCode,
            title,
            detail,
            type,
            new Dictionary<string, object?>
            {
                ["code"] = code,
                ["remainingAttempts"] = remainingAttempts,
                ["lockoutSeconds"] = lockoutSeconds
            });

        return TeamHubProblemDetailsFactory.ObjectResult(problem);
    }
}
