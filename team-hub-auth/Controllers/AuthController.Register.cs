using Medo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using team_hub_auth.Dtos;
using team_hub_auth.Models;

namespace team_hub_auth.Controllers;

public partial class AuthController
{
    static (string Username, string Name, string Surname, string Password) NormalizeRegisterInput(RegisterRequest req) =>
        ((req.Username ?? string.Empty).Trim(),
         (req.Name ?? string.Empty).Trim(),
         (req.Surname ?? string.Empty).Trim(),
         req.Password ?? string.Empty);

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var (username, name, surname, password) = NormalizeRegisterInput(req);

        logger.LogInformation("Registration attempt for username {Username}", username);

        var loweredUsername = username.ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Username.ToLower() == loweredUsername))
        {
            logger.LogWarning("Registration failed: username {Username} already exists", username);
            return Conflict(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["username"] = ["Username is already taken."]
            })
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Registration failed."
            });
        }

        var user = new User
        {
            Id = Uuid7.NewGuid(),
            Username = username,
            Name = name,
            Surname = surname,
            Password = passwordHasher.Hash(password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        logger.LogInformation("User {UserId} registered successfully with username {Username}", user.Id, user.Username);

        return Created($"/api/users/{user.Id}", ToResponse(user));
    }
}
