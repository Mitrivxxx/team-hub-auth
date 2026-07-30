using Microsoft.AspNetCore.Mvc;
using team_hub_auth.Dtos;
using team_hub_auth.Models;

namespace team_hub_auth.Tests.Controllers;

public class AuthControllerGetAllUsersTests
{
    [Fact]
    public async Task GetAllUsers_WhenUsersExist_ShouldReturnOrderedProfiles()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var bob = new User
        {
            Id = Guid.NewGuid(),
            Identity = new UserIdentity
            {
                Username = "bob",
                Email = ""
            },
            Profile = new UserProfile
            {
                Name = "Bob",
                Surname = "Jones"
            },
            Credentials = new UserCredentials
            {
                PasswordHash = "hash"
            }
        };
        var alice = new User
        {
            Id = Guid.NewGuid(),
            Identity = new UserIdentity
            {
                Username = "alice",
                Email = ""
            },
            Profile = new UserProfile
            {
                Name = "Alice",
                Surname = "Smith"
            },
            Credentials = new UserCredentials
            {
                PasswordHash = "hash"
            }
        };
        db.Users.AddRange(bob, alice);
        await db.SaveChangesAsync();

        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.GetAllUsers(cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsAssignableFrom<IReadOnlyList<UserResponse>>(ok.Value);
        Assert.Equal(2, users.Count);
        Assert.Equal("alice", users[0].Username);
        Assert.Equal("bob", users[1].Username);
        Assert.All(users, u => Assert.False(string.IsNullOrWhiteSpace(u.Id.ToString())));
    }

    [Fact]
    public async Task GetAllUsers_WhenNoUsers_ShouldReturnEmptyList()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.GetAllUsers(cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsAssignableFrom<IReadOnlyList<UserResponse>>(ok.Value);
        Assert.Empty(users);
    }

    [Fact]
    public async Task GetAllUsers_WithQuery_ShouldReturnMatchingUsers()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        db.Users.AddRange(
            new User
            {
                Id = Guid.NewGuid(),
                Identity = new UserIdentity { Username = "bob", Email = "bob@example.com" },
                Profile = new UserProfile { Name = "Bob", Surname = "Jones" },
                Credentials = new UserCredentials { PasswordHash = "hash" }
            },
            new User
            {
                Id = Guid.NewGuid(),
                Identity = new UserIdentity { Username = "alice", Email = "alice@teamhub.local" },
                Profile = new UserProfile { Name = "Alice", Surname = "Smith" },
                Credentials = new UserCredentials { PasswordHash = "hash" }
            });
        await db.SaveChangesAsync();

        var controller = AuthControllerTestHelpers.CreateController(db);

        var result = await controller.GetAllUsers(q: "alice", cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsAssignableFrom<IReadOnlyList<UserResponse>>(ok.Value);
        Assert.Single(users);
        Assert.Equal("alice", users[0].Username);
    }
}
