using System.Globalization;
using System.Text;
using Bogus;
using Medo;
using Microsoft.EntityFrameworkCore;
using team_hub_auth.Models;
using team_hub_auth.Services.Password;

namespace team_hub_auth.Data;

public sealed class DevDataSeeder(
    AuthDbContext db,
    IPasswordHasher passwordHasher,
    ILogger<DevDataSeeder> logger)
{
    const int UserCount = 10_000;
    const int BatchSize = 500;
    const string DemoPassword = "DemoPassword123!";
    const string EmailDomain = "@teamhub.local";
    const string ActiveUsername = "JanWilk123";
    const string ActivePassword = "janwilk123";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureActiveUserAsync(cancellationToken);

        var alreadySeeded = await db.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.Identity.Email.EndsWith(EmailDomain)
                     && u.Identity.Username != ActiveUsername,
                cancellationToken);

        if (alreadySeeded)
        {
            logger.LogInformation(
                "Demo user seed skipped: users with email domain '{Domain}' already exist",
                EmailDomain);
            return;
        }

        var passwordHash = passwordHasher.Hash(DemoPassword);
        var polishFaker = new Faker("pl");
        var englishFaker = new Faker("en");
        var usedUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ActiveUsername
        };

        logger.LogInformation("Seeding {Count} demo users (50% Polish names)...", UserCount);

        for (var offset = 0; offset < UserCount; offset += BatchSize)
        {
            var count = Math.Min(BatchSize, UserCount - offset);
            var batch = new List<User>(count);

            for (var i = 0; i < count; i++)
            {
                var n = offset + i + 1;
                var usePolish = n <= UserCount / 2;
                var faker = usePolish ? polishFaker : englishFaker;

                var name = faker.Name.FirstName();
                var surname = faker.Name.LastName();
                var baseUsername = BuildBaseUsername(name, surname);
                var username = MakeUniqueUsername(baseUsername, usedUsernames);

                batch.Add(new User
                {
                    Id = Uuid7.NewGuid(),
                    Identity = new UserIdentity
                    {
                        Username = username,
                        Email = $"{username}{EmailDomain}"
                    },
                    Profile = new UserProfile
                    {
                        Name = name,
                        Surname = surname
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

        logger.LogInformation("Dev seed completed: {Count} demo users created", UserCount);
    }

    async Task EnsureActiveUserAsync(CancellationToken cancellationToken)
    {
        var exists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Identity.Username == ActiveUsername, cancellationToken);

        if (exists)
        {
            logger.LogInformation(
                "Active login user '{Username}' already exists — skipped",
                ActiveUsername);
            return;
        }

        db.Users.Add(new User
        {
            Id = Uuid7.NewGuid(),
            Identity = new UserIdentity
            {
                Username = ActiveUsername,
                Email = $"{ActiveUsername.ToLowerInvariant()}{EmailDomain}"
            },
            Profile = new UserProfile
            {
                Name = "Jan",
                Surname = "Wilk"
            },
            Credentials = new UserCredentials
            {
                PasswordHash = passwordHasher.Hash(ActivePassword)
            }
        });

        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        logger.LogInformation(
            "Active login user created: username={Username} password={Password}",
            ActiveUsername,
            ActivePassword);
    }

    static string BuildBaseUsername(string name, string surname)
    {
        var namePart = TakeAsciiLetters(name, 3);
        var surnamePart = TakeAsciiLetters(surname, 3);
        return $"{namePart}_{surnamePart}".ToLowerInvariant();
    }

    static string MakeUniqueUsername(string baseUsername, HashSet<string> used)
    {
        if (used.Add(baseUsername))
            return baseUsername;

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseUsername}{suffix}";
            if (candidate.Length > 30)
                candidate = $"{baseUsername[..Math.Min(baseUsername.Length, 30 - suffix.ToString().Length)]}{suffix}";

            if (used.Add(candidate))
                return candidate;
        }
    }

    static string TakeAsciiLetters(string value, int count)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(count);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            var ascii = c switch
            {
                'ł' or 'Ł' => 'l',
                'ø' or 'Ø' => 'o',
                'æ' or 'Æ' => 'a',
                'ß' => 's',
                _ => c
            };

            if (ascii is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
            {
                sb.Append(ascii);
                if (sb.Length == count)
                    break;
            }
        }

        while (sb.Length < count)
            sb.Append('x');

        return sb.ToString();
    }
}
