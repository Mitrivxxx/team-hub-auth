using Medo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using team_hub_auth.Configuration;
using team_hub_auth.Data;
using team_hub_auth.Models;
using team_hub_auth.Services.Password;

namespace team_hub_auth.Seeding.Users;

public sealed class ActiveDemoUserSeeder(
    AuthDbContext db,
    IPasswordHasher passwordHasher,
    IOptions<SeedOptions> options,
    ILogger<ActiveDemoUserSeeder> logger)
{
    public async Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        var seed = options.Value;
        var username = seed.ActiveUsername;

        var exists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Identity.Username == username, cancellationToken);

        if (exists)
        {
            logger.LogInformation("Active login user '{Username}' already exists — skipped", username);
            return;
        }

        db.Users.Add(new User
        {
            Id = Uuid7.NewGuid(),
            Identity = new UserIdentity
            {
                Username = username,
                Email = $"{username.ToLowerInvariant()}{seed.EmailDomain}"
            },
            Profile = new UserProfile
            {
                Name = "Jan",
                Surname = "Wilk"
            },
            Credentials = new UserCredentials
            {
                PasswordHash = passwordHasher.Hash(seed.ActivePassword)
            }
        });

        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        logger.LogInformation("Active login user created: username={Username}", username);
    }
}
