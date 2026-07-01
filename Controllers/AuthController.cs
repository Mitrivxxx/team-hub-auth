using Medo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using team_hub_auth.Data;
using team_hub_auth.Dtos;
using team_hub_auth.Models;

namespace team_hub_auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthDbContext db) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Username == req.Username))
            return Conflict("Username already exists.");

        var roleName = req.Role ?? "user";
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role is null)
            return BadRequest($"Role '{roleName}' not found.");

        var user = new User
        {
            Id = Uuid7.NewGuid(),
            Username = req.Username,
            Name = req.Name,
            Surname = req.Surname,
            Password = req.Password,
            RoleId = role.Id
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Created($"/api/users/{user.Id}", ToResponse(user, role.Name));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == req.Username);

        if (user is null || user.Password != req.Password)
            return Unauthorized();

        return Ok(ToResponse(user, user.Role.Name));
    }

    static UserResponse ToResponse(User user, string role) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Name = user.Name,
        Surname = user.Surname,
        Role = role
    };
}
