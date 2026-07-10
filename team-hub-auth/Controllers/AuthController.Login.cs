using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text.Json;
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

        // #region agent log
        void AgentLog(string hypothesisId, string message, object data)
        {
            try
            {
                var payload = new
                {
                    sessionId = "76b50d",
                    runId = "pre-fix",
                    hypothesisId,
                    location = "AuthController.Login.cs:Login",
                    message,
                    data,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                System.IO.File.AppendAllText(
                    "/home/matsma/project/team-hub/services/team-hub-gateway/.cursor/debug-76b50d.log",
                    JsonSerializer.Serialize(payload) + Environment.NewLine);
            }
            catch
            {
                // ignore debug logging failures
            }
        }
        // #endregion

        logger.LogInformation("Login attempt for username {Username}", username);

        var now = DateTimeOffset.UtcNow;
        var connectionString = db.Database.GetConnectionString() ?? string.Empty;
        var csb = new DbConnectionStringBuilder { ConnectionString = connectionString };
        var host = csb.TryGetValue("Host", out var hostValue) ? hostValue?.ToString() : "unknown";
        var port = csb.TryGetValue("Port", out var portValue) ? portValue?.ToString() : "unknown";
        // #region agent log
        AgentLog("H1_DB_PORT", "Auth login DB target", new { host, port, env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") });
        // #endregion

        User? user;
        try
        {
            var loweredUsername = username.ToLower();
            user = await db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == loweredUsername);
        }
        catch (Exception ex)
        {
            // #region agent log
            AgentLog("H1_DB_PORT", "User lookup failed", new { exceptionType = ex.GetType().FullName, ex.Message, host, port });
            // #endregion
            throw;
        }

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

        var roleName = user.Role?.Name;
        var (accessToken, accessTokenExpiresAt) = tokenService.GenerateAccessToken(user, roleName);
        var (refreshToken, refreshTokenHash, refreshTokenExpiresAt) = tokenService.GenerateRefreshToken();

        user.RefreshTokenHash = refreshTokenHash;
        user.RefreshTokenExpiresAt = refreshTokenExpiresAt;
        await db.SaveChangesAsync();

        SetRefreshTokenCookie(refreshToken, refreshTokenExpiresAt, req.RememberMe);

        logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return Ok(ToAuthResponse(user, roleName, accessToken, accessTokenExpiresAt));
    }
}
