using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using team_hub_auth.Dtos;
using team_hub_auth.Models;
using team_hub_auth.Services;

namespace team_hub_auth.Tests.Controllers;

public class AuthControllerRegisterTests
{
    [Fact]
    public async Task Register_WhenUsernameAlreadyExists_ShouldReturnConflict()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var role = await AuthControllerTestHelpers.EnsureRoleAsync(db, "user");
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "john",
            Name = "John",
            Surname = "Doe",
            Password = PasswordHasher.Hash("secret123"),
            RoleId = role.Id
        });
        await db.SaveChangesAsync();

        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.Register(new RegisterRequest
        {
            Username = "john",
            Name = "Jane",
            Surname = "Doe",
            Password = "secret123",
            Role = "user"
        });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Register_WhenRoleDoesNotExist_ShouldReturnBadRequest()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.Register(new RegisterRequest
        {
            Username = "john",
            Name = "John",
            Surname = "Doe",
            Password = "secret123",
            Role = "guest"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_WhenRequestIsValid_ShouldReturnCreatedAndPersistUser()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        await AuthControllerTestHelpers.EnsureRoleAsync(db, "user");
        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.Register(new RegisterRequest
        {
            Username = "john",
            Name = "John",
            Surname = "Doe",
            Password = "secret123",
            Role = "user"
        });

        var created = Assert.IsType<CreatedResult>(result);
        var response = Assert.IsType<UserResponse>(created.Value);
        var user = await db.Users.Include(u => u.Role).SingleAsync(u => u.Username == "john");

        Assert.Equal("user", response.Role);
        Assert.NotEqual("secret123", user.Password);
        Assert.True(PasswordHasher.Verify("secret123", user.Password));
    }
}
