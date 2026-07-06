using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using team_hub_auth.Configuration;
using team_hub_auth.Controllers;
using team_hub_auth.Data;
using team_hub_auth.Models;
using team_hub_auth.Services.Password;
using team_hub_auth.Services.Tokens;

namespace team_hub_auth.Tests.Controllers;

internal static class AuthControllerTestHelpers
{
    public static readonly IPasswordHasher PasswordHasher = new PasswordHasher();

    public static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase($"auth-tests-{Guid.NewGuid():N}")
            .Options;

        var db = new AuthDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static async Task<Role> EnsureRoleAsync(AuthDbContext db, string roleName)
    {
        var existing = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (existing is not null)
            return existing;

        var role = new Role { Name = roleName };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role;
    }

    public static AuthController CreateController(
        AuthDbContext db,
        string? requestCookie = null,
        ITokenService? tokenService = null,
        IPasswordHasher? passwordHasher = null)
    {
        tokenService ??= CreateTokenService(expireMinutes: 15);
        passwordHasher ??= PasswordHasher;

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment { EnvironmentName = Environments.Development });
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        if (!string.IsNullOrWhiteSpace(requestCookie))
            httpContext.Request.Headers.Cookie = requestCookie;

        return new AuthController(db, tokenService, passwordHasher, NullLogger<AuthController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    public static TokenService CreateTokenService(int expireMinutes)
    {
        var options = Options.Create(new JwtOptions
        {
            Key = "super-secret-test-key-that-is-long-enough",
            Issuer = "TeamHubTests",
            Audience = "TeamHubTestsAudience",
            ExpireMinutes = expireMinutes
        });

        return new TokenService(options);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "team-hub-auth-tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
