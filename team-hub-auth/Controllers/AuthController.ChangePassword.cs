using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using team_hub_auth.Dtos;

namespace team_hub_auth.Controllers;

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
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == loweredUsername);

        if (user is null
            || !string.Equals(user.Name, name, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(user.Surname, surname, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Password change failed for username {Username}", username);
            return Unauthorized();
        }

        user.Password = passwordHasher.Hash(password);
        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        await db.SaveChangesAsync();

        logger.LogInformation("User {UserId} changed password successfully", user.Id);

        return NoContent();
    }
}
