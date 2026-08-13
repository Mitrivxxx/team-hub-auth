using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TeamHub.BlobStorage;
using team_hub_auth.Configuration;
using team_hub_auth.Tests.Configuration;
using team_hub_auth.Controllers;
using team_hub_auth.Data;
using team_hub_auth.Models;
using team_hub_auth.Services.LoginAttempts;
using team_hub_auth.Services.Password;
using team_hub_auth.Services.Sessions;
using team_hub_auth.Services.Tokens;
using team_hub_auth.Services.Users;

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

    public static AuthController CreateController(
        AuthDbContext db,
        string? requestCookie = null,
        ITokenService? tokenService = null,
        ISessionStore? sessionStore = null,
        IPasswordHasher? passwordHasher = null,
        ILoginAttemptLimiter? loginAttemptLimiter = null,
        IUserQueryService? userQueryService = null,
        IUserResponseMapper? userResponseMapper = null)
    {
        tokenService ??= CreateTokenService(expireMinutes: 15);
        sessionStore ??= new InMemorySessionStore();
        passwordHasher ??= PasswordHasher;
        loginAttemptLimiter ??= new InMemoryLoginAttemptLimiter();
        userQueryService ??= new UserQueryService(db);
        userResponseMapper ??= CreateUserResponseMapper();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment { EnvironmentName = Environments.Development });
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        if (!string.IsNullOrWhiteSpace(requestCookie))
            httpContext.Request.Headers.Cookie = requestCookie;

        return new AuthController(
            db,
            tokenService,
            sessionStore,
            passwordHasher,
            loginAttemptLimiter,
            userQueryService,
            userResponseMapper,
            NullLogger<AuthController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    public static IUserResponseMapper CreateUserResponseMapper(IBlobStorageService? blobStorage = null)
    {
        var services = new ServiceCollection();
        if (blobStorage is not null)
            services.AddSingleton(blobStorage);
        return new UserResponseMapper(services.BuildServiceProvider());
    }

    public static TokenService CreateTokenService(int expireMinutes)
    {
        var options = TestJwtConfiguration.CreateJwtOptions(expireMinutes);
        return new TokenService(options);
    }

    public static string? GetSetCookieValue(IHeaderDictionary headers, string cookieName)
    {
        var marker = $"{cookieName}=";
        foreach (var header in headers.SetCookie)
        {
            if (header is null || !header.StartsWith(marker, StringComparison.Ordinal))
                continue;

            var valueStart = marker.Length;
            var valueEnd = header.IndexOf(';', valueStart);
            var rawValue = valueEnd < 0 ? header[valueStart..] : header[valueStart..valueEnd];
            return Uri.UnescapeDataString(rawValue);
        }

        return null;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "team-hub-auth-tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Directory.GetCurrentDirectory());
    }

    sealed class InMemoryLoginAttemptLimiter : ILoginAttemptLimiter
    {
        readonly Dictionary<string, int> attemptsByUsername = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, DateTimeOffset> lockoutUntilByUsername = new(StringComparer.OrdinalIgnoreCase);

        public Task<LoginLockoutStatus> GetLockoutStatusAsync(string username, CancellationToken cancellationToken = default)
        {
            if (lockoutUntilByUsername.TryGetValue(username, out var lockoutUntil) && lockoutUntil > DateTimeOffset.UtcNow)
            {
                var remainingSeconds = (int)Math.Ceiling((lockoutUntil - DateTimeOffset.UtcNow).TotalSeconds);
                return Task.FromResult(new LoginLockoutStatus(IsLocked: true, LockoutSeconds: Math.Max(0, remainingSeconds)));
            }

            return Task.FromResult(new LoginLockoutStatus(IsLocked: false, LockoutSeconds: 0));
        }

        public Task<LoginFailureOutcome> RegisterFailedAttemptAsync(
            string username,
            int maxFailedAttempts,
            TimeSpan lockoutDuration,
            CancellationToken cancellationToken = default)
        {
            attemptsByUsername.TryGetValue(username, out var attempts);
            attempts++;

            if (attempts >= maxFailedAttempts)
            {
                lockoutUntilByUsername[username] = DateTimeOffset.UtcNow.Add(lockoutDuration);
                attemptsByUsername[username] = 0;
                return Task.FromResult(new LoginFailureOutcome(
                    RemainingAttempts: 0,
                    IsLocked: true,
                    LockoutSeconds: (int)Math.Ceiling(lockoutDuration.TotalSeconds)));
            }

            attemptsByUsername[username] = attempts;
            var remainingAttempts = Math.Max(0, maxFailedAttempts - attempts);
            return Task.FromResult(new LoginFailureOutcome(
                RemainingAttempts: remainingAttempts,
                IsLocked: false,
                LockoutSeconds: 0));
        }

        public Task ClearAttemptsAsync(string username, CancellationToken cancellationToken = default)
        {
            attemptsByUsername.Remove(username);
            lockoutUntilByUsername.Remove(username);
            return Task.CompletedTask;
        }
    }
}
