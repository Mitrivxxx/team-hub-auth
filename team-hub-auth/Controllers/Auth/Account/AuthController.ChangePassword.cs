using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamHub.Observability;
using team_hub_auth.Dtos;

namespace team_hub_auth.Controllers.Auth;

public partial class AuthController
{
    static (string Username, string Name, string Surname, string Password) NormalizeChangePasswordInput(
        ChangePasswordRequest req) =>
        ((req.Username ?? string.Empty).Trim(),
         (req.Name ?? string.Empty).Trim(),
         (req.Surname ?? string.Empty).Trim(),
         req.Password ?? string.Empty);

    /// <summary>Change password after identity verification.</summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        var (username, name, surname, password) = NormalizeChangePasswordInput(req);

        logger.LogInformation("Password change attempt for username {Username}", username);

        var loweredUsername = username.ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Identity.Username.ToLower() == loweredUsername);

        if (user is null
            || !string.Equals(user.Profile.Name, name, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(user.Profile.Surname, surname, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Password change failed for username {Username}", username);
            return TeamHubProblemDetailsFactory.ObjectResult(TeamHubProblemDetailsFactory.Create(
                HttpContext,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Identity verification failed.",
                ProblemTypes.Unauthorized));
        }

        user.Credentials.PasswordHash = passwordHasher.Hash(password);
        user.Security.FailedLoginAttempts = 0;
        user.Security.LockoutUntil = null;
        await db.SaveChangesAsync();
        await sessionStore.RevokeAllSessionsAsync(user.Id);
        DeleteRefreshTokenCookie();

        logger.LogInformation("User {UserId} changed password successfully", user.Id);

        return NoContent();
    }
}
