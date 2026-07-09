using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
}
