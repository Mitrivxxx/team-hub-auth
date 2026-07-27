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
            Username = "alice",
            Name = "Alice",
            Surname = "Smith",
            Password = "hash"
        };
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Username = "bob",
            Name = "Bob",
            Surname = "Jones",
            Password = "hash"
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
