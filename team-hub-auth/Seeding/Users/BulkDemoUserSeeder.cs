using Bogus;
using Medo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using team_hub_auth.Configuration;
using team_hub_auth.Data;
using team_hub_auth.Models;
using team_hub_auth.Services.Password;

namespace team_hub_auth.Seeding.Users;

public sealed class BulkDemoUserSeeder(
    AuthDbContext db,
    IPasswordHasher passwordHasher,
    IOptions<SeedOptions> options,
    ILogger<BulkDemoUserSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seed = options.Value;
        var userCount = seed.UserCount;
        if (userCount <= 0)
        {
            logger.LogInformation("Bulk demo user seed skipped: UserCount is 0");
            return;
        }

        var alreadySeeded = await db.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.Identity.Email.EndsWith(seed.EmailDomain)
                     && u.Identity.Username != seed.ActiveUsername,
                cancellationToken);

        if (alreadySeeded)
        {
            logger.LogInformation(
                "Demo user seed skipped: users with email domain '{Domain}' already exist",
                seed.EmailDomain);
            return;
        }

        var passwordHash = passwordHasher.Hash(seed.DemoPassword);
        var polishFaker = new Faker("pl");
        var englishFaker = new Faker("en");
        var batchSize = seed.BatchSize;

        logger.LogInformation(
            "Seeding {Count} demo users (deterministic demoNNNNN usernames, 50% Polish names)...",
            userCount);

        for (var offset = 0; offset < userCount; offset += batchSize)
        {
            var count = Math.Min(batchSize, userCount - offset);
            var batch = new List<User>(count);

            for (var i = 0; i < count; i++)
            {
                var n = offset + i + 1;
                var usePolish = n <= userCount / 2;
                var faker = usePolish ? polishFaker : englishFaker;
                var username = $"demo{n:D5}";

                batch.Add(new User
                {
                    Id = Uuid7.NewGuid(),
                    Identity = new UserIdentity
                    {
                        Username = username,
                        Email = $"{username}{seed.EmailDomain}"
                    },
                    Profile = new UserProfile
                    {
                        Name = faker.Name.FirstName(),
                        Surname = faker.Name.LastName()
                    },
                    Credentials = new UserCredentials
                    {
                        PasswordHash = passwordHash
                    }
                });
            }

            db.Users.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }

        logger.LogInformation("Bulk demo user seed completed: {Count} users created", userCount);
    }
}
