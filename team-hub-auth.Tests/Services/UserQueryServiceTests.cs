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

    [Fact]
    public async Task GetAllUsersAsync_WithQuery_MatchesNameSurnameOrEmail()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            CreateUser("amy", "Amy", "Ace", "amy@teamhub.local"),
            CreateUser("bob", "Robert", "Bee", "bob@example.com"),
            CreateUser("zoe", "Zoe", "Wilk", "zoe@teamhub.local"));
        await db.SaveChangesAsync();

        var service = new UserQueryService(db);

        var byName = await service.GetAllUsersAsync(q: "amy");
        var bySurname = await service.GetAllUsersAsync(q: "WILK");
        var byEmail = await service.GetAllUsersAsync(q: "bob@example");

        Assert.Single(byName);
        Assert.Equal("amy", byName[0].Username);
        Assert.Single(bySurname);
        Assert.Equal("zoe", bySurname[0].Username);
        Assert.Single(byEmail);
        Assert.Equal("bob", byEmail[0].Username);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithFullNameQuery_MatchesNameAndSurname()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            CreateUser("amy", "Amy", "Ace", "amy@teamhub.local"),
            CreateUser("bob", "Robert", "Bee", "bob@example.com"),
            CreateUser("jan", "Jan", "Kowalski", "jan@teamhub.local"));
        await db.SaveChangesAsync();

        var service = new UserQueryService(db);

        var byFullName = await service.GetAllUsersAsync(q: "Jan Kowalski");
        var byReversed = await service.GetAllUsersAsync(q: "kowalski jan");

        Assert.Single(byFullName);
        Assert.Equal("jan", byFullName[0].Username);
        Assert.Single(byReversed);
        Assert.Equal("jan", byReversed[0].Username);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithWhitespaceQuery_ReturnsAll()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            CreateUser("amy", "Amy", "Ace"),
            CreateUser("bob", "Bob", "Bee"));
        await db.SaveChangesAsync();

        var service = new UserQueryService(db);

        var result = await service.GetAllUsersAsync(q: "   ");

        Assert.Equal(2, result.Count);
    }

    static User CreateUser(string username, string name, string surname, string email = "") => new()
    {
        Id = Guid.NewGuid(),
        Identity = new UserIdentity
        {
            Username = username,
            Email = email
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
