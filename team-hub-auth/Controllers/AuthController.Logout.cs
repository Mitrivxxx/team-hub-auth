using Microsoft.AspNetCore.Mvc;

namespace team_hub_auth.Controllers;

public partial class AuthController
{
    /// <summary>Sign out and revoke refresh token.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var refreshTokenHash = tokenService.HashRefreshToken(refreshToken);
            await sessionStore.RevokeRefreshSessionAsync(refreshTokenHash);
        }

        DeleteRefreshTokenCookie();
        return NoContent();
    }
}
