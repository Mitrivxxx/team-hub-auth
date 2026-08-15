using Microsoft.AspNetCore.Mvc;

namespace team_hub_auth.Controllers.Auth;

public partial class AuthController
{
    /// <summary>Sign out and revoke refresh token.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            logger.LogInformation("Logout completed with no refresh token cookie");
            DeleteRefreshTokenCookie();
            return NoContent();
        }

        var refreshTokenHash = tokenService.HashRefreshToken(refreshToken);
        await sessionStore.RevokeRefreshSessionAsync(refreshTokenHash);
        DeleteRefreshTokenCookie();
        logger.LogInformation("Refresh session revoked on logout");
        return NoContent();
    }
}
