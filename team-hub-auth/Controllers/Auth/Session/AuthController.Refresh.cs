using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamHub.Observability;

namespace team_hub_auth.Controllers.Auth;

public partial class AuthController
{
    /// <summary>Refresh access token using cookie.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        var rememberMe = string.Equals(
            Request.Cookies[RefreshTokenPersistentCookieName],
            "1",
            StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            logger.LogWarning("Refresh failed: refresh token cookie is missing");
            return UnauthorizedProblem("Refresh token is missing.");
        }

        var refreshTokenHash = tokenService.HashRefreshToken(refreshToken);
        var session = await sessionStore.GetRefreshSessionAsync(refreshTokenHash);
        if (session is null)
        {
            logger.LogWarning("Refresh failed: refresh token is invalid or expired");
            DeleteRefreshTokenCookie();
            return UnauthorizedProblem("Refresh token is invalid or expired.");
        }

        rememberMe = session.RememberMe;

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == session.UserId);

        if (user is null)
        {
            logger.LogWarning("Refresh failed: session user {UserId} was not found", session.UserId);
            await sessionStore.RevokeRefreshSessionAsync(refreshTokenHash);
            DeleteRefreshTokenCookie();
            return UnauthorizedProblem("Refresh session user was not found.");
        }

        var (accessToken, accessTokenExpiresAt) = tokenService.GenerateAccessToken(user);
        var (newRefreshToken, newRefreshTokenHash, newRefreshTokenExpiresAt) = tokenService.GenerateRefreshToken();

        await sessionStore.RevokeRefreshSessionAsync(refreshTokenHash);
        await sessionStore.StoreRefreshSessionAsync(
            newRefreshTokenHash,
            user.Id,
            rememberMe,
            newRefreshTokenExpiresAt);

        SetRefreshTokenCookie(newRefreshToken, newRefreshTokenExpiresAt, rememberMe);

        logger.LogInformation("User {UserId} refreshed tokens successfully", user.Id);

        return Ok(ToAuthResponse(user, accessToken, accessTokenExpiresAt));
    }

    IActionResult UnauthorizedProblem(string detail) =>
        TeamHubProblemDetailsFactory.ObjectResult(TeamHubProblemDetailsFactory.Create(
            HttpContext,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            detail,
            ProblemTypes.Unauthorized));
}
