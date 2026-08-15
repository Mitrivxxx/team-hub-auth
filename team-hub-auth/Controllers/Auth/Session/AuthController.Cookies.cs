namespace team_hub_auth.Controllers.Auth;

public partial class AuthController
{
    void SetRefreshTokenCookie(string refreshToken, DateTimeOffset expiresAt, bool rememberMe)
    {
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = rememberMe ? expiresAt : null
        });

        Response.Cookies.Append(RefreshTokenPersistentCookieName, rememberMe ? "1" : "0", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = rememberMe ? expiresAt : null
        });
    }

    void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
        Response.Cookies.Delete(RefreshTokenPersistentCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }
}
