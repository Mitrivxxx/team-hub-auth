using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace team_hub_auth.Controllers;

public partial class AuthController
{
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
}
