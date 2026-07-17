using Microsoft.AspNetCore.Mvc;
using team_hub_auth.Data;
using team_hub_auth.Services.LoginAttempts;
using team_hub_auth.Services.Password;
using team_hub_auth.Services.Sessions;
using team_hub_auth.Services.Tokens;

namespace team_hub_auth.Controllers;

[ApiController]
[Route("api/auth")]
public partial class AuthController(
    AuthDbContext db,
    ITokenService tokenService,
    ISessionStore sessionStore,
    IPasswordHasher passwordHasher,
    ILoginAttemptLimiter loginAttemptLimiter,
    ILogger<AuthController> logger) : ControllerBase
{
    const string RefreshTokenCookieName = "refreshToken";
    const string RefreshTokenPersistentCookieName = "refreshTokenPersistent";
    const int MaxFailedLoginAttempts = 5;
    static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
}
