using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using team_hub_auth.Dtos;
using team_hub_auth.Models;

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
            Password = AuthControllerTestHelpers.PasswordHasher.Hash("secret123"),
            RoleId = role.Id
        });
        await db.SaveChangesAsync();

        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.Register(new RegisterRequest
        {
            Username = "john",
            Name = "Jane",
            Surname = "Doe",
            Password = "secret123"
        });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Register_WhenRequestIsValid_ShouldReturnCreatedAndPersistUser()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.Register(new RegisterRequest
        {
            Username = "john",
            Name = "John",
            Surname = "Doe",
            Password = "secret123"
        });

        var created = Assert.IsType<CreatedResult>(result);
        var response = Assert.IsType<UserResponse>(created.Value);
        var user = await db.Users.SingleAsync(u => u.Username == "john");

        Assert.Null(response.Role);
        Assert.Null(user.RoleId);
        Assert.NotEqual("secret123", user.Password);
        Assert.True(AuthControllerTestHelpers.PasswordHasher.Verify("secret123", user.Password));
    }
}
