using Microsoft.EntityFrameworkCore;
using team_hub_auth.Data;
using team_hub_auth.Models;
using team_hub_auth.Services.Users;

namespace team_hub_auth.Tests.Services;

public class UserQueryServiceTests
{
    [Fact]
    public async Task GetUsersByIdsAsync_ReturnsMatchingProfilesOnly()
    {
        await using var db = CreateDb();
        var user1 = new User
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
        var user2 = new User
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
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var service = new UserQueryService(db);
        var result = await service.GetUsersByIdsAsync([user1.Id, Guid.NewGuid(), user2.Id]);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Id == user1.Id && u.Name == "Alice" && u.Surname == "Smith");
        Assert.Contains(result, u => u.Id == user2.Id && u.Username == "bob");
    }

    [Fact]
    public async Task GetUsersByIdsAsync_WhenEmpty_ReturnsEmpty()
    {
        await using var db = CreateDb();
        var service = new UserQueryService(db);

        var result = await service.GetUsersByIdsAsync([]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsAllProfilesOrderedByUsername()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            new User
            {
                Id = Guid.NewGuid(),
                Identity = new UserIdentity
                {
                    Username = "zoe",
                    Email = ""
                },
                Profile = new UserProfile
                {
                    Name = "Zoe",
                    Surname = "Zed"
                },
                Credentials = new UserCredentials
                {
                    PasswordHash = "hash"
                }
            },
            new User
            {
                Id = Guid.NewGuid(),
                Identity = new UserIdentity
                {
                    Username = "amy",
                    Email = ""
                },
                Profile = new UserProfile
                {
                    Name = "Amy",
                    Surname = "Ace"
                },
                Credentials = new UserCredentials
                {
                    PasswordHash = "hash"
                }
            });
        await db.SaveChangesAsync();

        var service = new UserQueryService(db);
        var result = await service.GetAllUsersAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("amy", result[0].Username);
        Assert.Equal("zoe", result[1].Username);
    }

    [Fact]
    public async Task GetAllUsersAsync_WhenEmpty_ReturnsEmpty()
    {
        await using var db = CreateDb();
        var service = new UserQueryService(db);

        var result = await service.GetAllUsersAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithPagination_ReturnsRequestedPage()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            CreateUser("amy", "Amy", "Ace"),
            CreateUser("bob", "Bob", "Bee"),
            CreateUser("zoe", "Zoe", "Zed"));
        await db.SaveChangesAsync();

        var service = new UserQueryService(db);

        var page1 = await service.GetAllUsersAsync(page: 1, pageSize: 2);
        var page2 = await service.GetAllUsersAsync(page: 2, pageSize: 2);

        Assert.Equal(2, page1.Count);
        Assert.Equal("amy", page1[0].Username);
        Assert.Equal("bob", page1[1].Username);
        Assert.Single(page2);
        Assert.Equal("zoe", page2[0].Username);
    }

    [Fact]
    public async Task GetAllUsersAsync_ClampsInvalidPageAndPageSize()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            CreateUser("amy", "Amy", "Ace"),
            CreateUser("bob", "Bob", "Bee"));
        await db.SaveChangesAsync();

        var service = new UserQueryService(db);

        var result = await service.GetAllUsersAsync(page: 0, pageSize: 0);

        Assert.Equal(2, result.Count);
        Assert.Equal("amy", result[0].Username);
    }

    static User CreateUser(string username, string name, string surname) => new()
    {
        Id = Guid.NewGuid(),
        Identity = new UserIdentity
        {
            Username = username,
            Email = ""
        },
        Profile = new UserProfile
        {
            Name = name,
            Surname = surname
        },
        Credentials = new UserCredentials
        {
            PasswordHash = "hash"
        }
    };

    static AuthDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase($"user-query-{Guid.NewGuid():N}")
            .Options;
        var db = new AuthDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
