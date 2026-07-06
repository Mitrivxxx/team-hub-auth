using Medo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using team_hub_auth.Dtos;
using team_hub_auth.Models;

namespace team_hub_auth.Controllers;

public partial class AuthController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        logger.LogInformation("Registration attempt for username {Username}", req.Username);

        if (await db.Users.AnyAsync(u => u.Username == req.Username))
        {
            logger.LogWarning("Registration failed: username {Username} already exists", req.Username);
            return Conflict("Username already exists.");
        }

        var user = new User
        {
            Id = Uuid7.NewGuid(),
            Username = req.Username,
            Name = req.Name,
            Surname = req.Surname,
            Password = passwordHasher.Hash(req.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        logger.LogInformation("User {UserId} registered successfully with username {Username}", user.Id, user.Username);

        return Created($"/api/users/{user.Id}", ToResponse(user, null));
    }
}
