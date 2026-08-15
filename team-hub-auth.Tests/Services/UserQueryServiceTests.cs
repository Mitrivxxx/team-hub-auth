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
    public async Task GetAllUsersAsync_WithoutQuery_ReturnsEmpty()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            CreateUser("zoe", "Zoe", "Zed"),
            CreateUser("amy", "Amy", "Ace"));
        await db.SaveChangesAsync();

        var service = new UserQueryService(db);
        var result = await service.GetAllUsersAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithShortQuery_ReturnsEmpty()
    {
        await using var db = CreateDb();
        db.Users.Add(CreateUser("amy", "Amy", "Ace"));
        await db.SaveChangesAsync();

        var service = new UserQueryService(db);

        Assert.Empty(await service.GetAllUsersAsync(q: "a"));
        Assert.Empty(await service.GetAllUsersAsync(q: " "));
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

        var page1 = await service.GetAllUsersAsync(page: 1, pageSize: 2, q: "e");
        // single-char q is rejected
        Assert.Empty(page1);

        var byAmy = await service.GetAllUsersAsync(page: 1, pageSize: 2, q: "am");
        Assert.Single(byAmy);
        Assert.Equal("amy", byAmy[0].Username);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithQuery_MatchesNameSurnameOrUsername_OmitsEmail()
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
        var byUsername = await service.GetAllUsersAsync(q: "bob");
        var byEmail = await service.GetAllUsersAsync(q: "bob@example");

        Assert.Single(byName);
        Assert.Equal("amy", byName[0].Username);
        Assert.Equal("", byName[0].Email);
        Assert.Single(bySurname);
        Assert.Equal("zoe", bySurname[0].Username);
        Assert.Single(byUsername);
        Assert.Equal("bob", byUsername[0].Username);
        Assert.Empty(byEmail);
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
        Assert.Equal("", byFullName[0].Email);
        Assert.Single(byReversed);
        Assert.Equal("jan", byReversed[0].Username);
    }

    [Fact]
    public async Task ResolveUsersAsync_MatchesEmailAndUsername_CaseInsensitive()
    {
        await using var db = CreateDb();
        var byEmail = CreateUser("alice", "Alice", "Smith", "alice@example.com");
        var byUsername = CreateUser("bob", "Bob", "Jones", "bob@example.com");
        db.Users.AddRange(byEmail, byUsername);
        await db.SaveChangesAsync();

        var service = new UserQueryService(db);
        var result = await service.ResolveUsersAsync(
            ["ALICE@EXAMPLE.COM"],
            ["Bob"]);

        Assert.Equal(2, result.Users.Count);
        Assert.Empty(result.UnresolvedEmails);
        Assert.Empty(result.UnresolvedUsernames);
        Assert.Contains(result.Users, u => u.Id == byEmail.Id);
        Assert.Contains(result.Users, u => u.Id == byUsername.Id);
    }

    [Fact]
    public async Task ResolveUsersAsync_ReturnsUnresolvedIdentifiers()
    {
        await using var db = CreateDb();
        db.Users.Add(CreateUser("alice", "Alice", "Smith", "alice@example.com"));
        await db.SaveChangesAsync();

        var service = new UserQueryService(db);
        var result = await service.ResolveUsersAsync(
            ["missing@example.com", "alice@example.com"],
            ["ghost"]);

        Assert.Single(result.Users);
        Assert.Equal(["missing@example.com"], result.UnresolvedEmails);
        Assert.Equal(["ghost"], result.UnresolvedUsernames);
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
