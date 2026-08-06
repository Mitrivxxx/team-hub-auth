using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamHub.Observability;
using team_hub_auth.Dtos;
using team_hub_auth.Models;

namespace team_hub_auth.Tests.Controllers;

public class AuthControllerRegisterTests
{
    [Fact]
    public async Task Register_WhenUsernameAlreadyExists_ShouldReturnConflict()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Identity = new UserIdentity
            {
                Username = "john",
                Email = ""
            },
            Profile = new UserProfile
            {
                Name = "John",
                Surname = "Doe"
            },
            Credentials = new UserCredentials
            {
                PasswordHash = AuthControllerTestHelpers.PasswordHasher.Hash("secret123")
            }
        });
        await db.SaveChangesAsync();

        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.Register(new RegisterRequest
        {
            Username = "john",
            Email = "jane@example.com",
            Name = "Jane",
            Surname = "Doe",
            Password = "secret123456"
        });

        Assert.IsType<ObjectResult>(result);
        var conflict = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(conflict.Value);
        Assert.Equal(ProblemTypes.Conflict, problem.Type);
        Assert.True(problem.Errors.ContainsKey("username"));
    }

    [Fact]
    public async Task Register_WhenRequestIsValid_ShouldReturnCreatedAndPersistUser()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.Register(new RegisterRequest
        {
            Username = "john",
            Email = "john@example.com",
            Name = "John",
            Surname = "Doe",
            Password = "secret123456"
        });

        var created = Assert.IsType<CreatedResult>(result);
        Assert.IsType<UserResponse>(created.Value);
        var user = await db.Users.SingleAsync(u => u.Identity.Username == "john");

        Assert.NotEqual("secret123", user.Credentials.PasswordHash);
        Assert.True(AuthControllerTestHelpers.PasswordHasher.Verify("secret123456", user.Credentials.PasswordHash));
        Assert.Equal("john@example.com", user.Identity.Email);
    }
}
