using Microsoft.AspNetCore.Mvc;
using team_hub_auth.Data;
using team_hub_auth.Services.Password;
using team_hub_auth.Services.Tokens;

namespace team_hub_auth.Controllers;

[ApiController]
[Route("api/auth")]
public partial class AuthController(
    AuthDbContext db,
    ITokenService tokenService,
    IPasswordHasher passwordHasher,
    ILogger<AuthController> logger) : ControllerBase
{
    const string RefreshTokenCookieName = "refreshToken";
}
