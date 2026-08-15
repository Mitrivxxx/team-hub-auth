using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using TeamHub.Observability;
using team_hub_auth.Controllers.Me;
using team_hub_auth.Data;
using team_hub_auth.Tests.Controllers.Auth;
using team_hub_auth.Dtos;
using team_hub_auth.Models;
using team_hub_auth.Services;
using team_hub_auth.Services.Users;

namespace team_hub_auth.Tests.Controllers.Me;

public class MeControllerTests
{
    [Fact]
    public async Task GetMe_WhenUnauthenticated_ReturnsUnauthorized()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var controller = CreateController(db, userId: null);

        var result = await controller.GetMe();

        var unauthorized = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task GetMe_WhenUserExists_ReturnsProfile()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var user = AddUser(db, "alice", "alice@example.com", "Alice", "Smith");
        await db.SaveChangesAsync();
        var controller = CreateController(db, user.Id);

        var result = await controller.GetMe();

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<UserResponse>(ok.Value);
        Assert.Equal(user.Id, body.Id);
        Assert.Equal("alice", body.Username);
        Assert.Equal("Alice", body.Name);
        Assert.Equal("Smith", body.Surname);
        Assert.Equal("alice@example.com", body.Email);
    }

    [Fact]
    public async Task UpdateMe_WhenNameProvided_UpdatesProfile()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var user = AddUser(db, "alice", "alice@example.com", "Alice", "Smith");
        await db.SaveChangesAsync();
        var controller = CreateController(db, user.Id);

        var result = await controller.UpdateMe(new UpdateMeRequest { Name = "Alicja" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<UserResponse>(ok.Value);
        Assert.Equal("Alicja", body.Name);
        Assert.Equal("Smith", body.Surname);
    }

    [Fact]
    public async Task UpdateMe_WhenEmailTaken_ReturnsConflict()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var user = AddUser(db, "alice", "alice@example.com", "Alice", "Smith");
        AddUser(db, "bob", "bob@example.com", "Bob", "Jones");
        await db.SaveChangesAsync();
        var controller = CreateController(db, user.Id);

        var result = await controller.UpdateMe(new UpdateMeRequest { Email = "bob@example.com" });

        var conflict = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(conflict.Value);
        Assert.Equal(ProblemTypes.Conflict, problem.Type);
        Assert.True(problem.Errors.ContainsKey("email"));
    }

    [Fact]
    public async Task ChangeMyPassword_WhenCurrentPasswordInvalid_ReturnsUnauthorized()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var user = AddUser(db, "alice", "alice@example.com", "Alice", "Smith", "secret123");
        await db.SaveChangesAsync();
        var controller = CreateController(db, user.Id);

        var result = await controller.ChangeMyPassword(new ChangeMyPasswordRequest
        {
            CurrentPassword = "wrong-password",
            NewPassword = "newsecret1"
        });

        var unauthorized = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task ChangeMyPassword_WhenCurrentPasswordValid_ReturnsNoContent()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var user = AddUser(db, "alice", "alice@example.com", "Alice", "Smith", "secret123");
        await db.SaveChangesAsync();
        var sessionStore = new team_hub_auth.Services.Sessions.InMemorySessionStore();
        await sessionStore.StoreRefreshSessionAsync(
            "hash-1",
            user.Id,
            rememberMe: true,
            DateTimeOffset.UtcNow.AddHours(1));
        var controller = CreateController(db, user.Id, sessionStore);

        var result = await controller.ChangeMyPassword(new ChangeMyPasswordRequest
        {
            CurrentPassword = "secret123",
            NewPassword = "newsecret1"
        });

        Assert.IsType<NoContentResult>(result);
        Assert.True(AuthControllerTestHelpers.PasswordHasher.Verify("newsecret1", user.Credentials.PasswordHash));
        Assert.Null(await sessionStore.GetRefreshSessionAsync("hash-1"));
    }

    [Fact]
    public async Task UpdateMe_WhenEmailChanges_RevokesSessions()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var user = AddUser(db, "alice", "alice@example.com", "Alice", "Smith");
        await db.SaveChangesAsync();
        var sessionStore = new team_hub_auth.Services.Sessions.InMemorySessionStore();
        await sessionStore.StoreRefreshSessionAsync(
            "hash-1",
            user.Id,
            rememberMe: true,
            DateTimeOffset.UtcNow.AddHours(1));
        var controller = CreateController(db, user.Id, sessionStore);

        var result = await controller.UpdateMe(new UpdateMeRequest { Email = "alice2@example.com" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<UserResponse>(ok.Value);
        Assert.Equal("alice2@example.com", body.Email);
        Assert.Null(await sessionStore.GetRefreshSessionAsync("hash-1"));
    }

    static User AddUser(
        AuthDbContext db,
        string username,
        string email,
        string name,
        string surname,
        string password = "secret123")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Identity = new UserIdentity { Username = username, Email = email },
            Profile = new UserProfile { Name = name, Surname = surname },
            Credentials = new UserCredentials
            {
                PasswordHash = AuthControllerTestHelpers.PasswordHasher.Hash(password)
            }
        };
        db.Users.Add(user);
        return user;
    }

    static MeController CreateController(
        AuthDbContext db,
        Guid? userId,
        team_hub_auth.Services.Sessions.ISessionStore? sessionStore = null)
    {
        var mapper = AuthControllerTestHelpers.CreateUserResponseMapper();
        sessionStore ??= new team_hub_auth.Services.Sessions.InMemorySessionStore();
        var profileService = new MeProfileService(db, AuthControllerTestHelpers.PasswordHasher, mapper, sessionStore);
        var avatarService = new UserAvatarService(db, mapper, new ServiceCollection().BuildServiceProvider());

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        if (userId is Guid id)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", id.ToString())],
                authenticationType: "Test"));
        }

        return new MeController(
            new FakeCurrentUserService(userId),
            profileService,
            avatarService,
            NullLogger<MeController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    sealed class FakeCurrentUserService(Guid? userId) : ICurrentUserService
    {
        public Guid GetRequiredUserId() =>
            userId ?? throw new UnauthorizedAccessException("User is not authenticated.");

        public bool TryGetUserId(out Guid parsed)
        {
            parsed = userId ?? default;
            return userId is not null;
        }
    }

    sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "team-hub-auth-tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
