using team_hub_auth.Seeding.Abstractions;
using team_hub_auth.Seeding.Users;

namespace team_hub_auth.Seeding.Development;

public sealed class DevelopmentDataSeeder(
    ActiveDemoUserSeeder activeUserSeeder,
    BulkDemoUserSeeder bulkUserSeeder,
    ILogger<DevelopmentDataSeeder> logger) : IEnvironmentDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running Development auth data seed...");
        await activeUserSeeder.EnsureAsync(cancellationToken);
        await bulkUserSeeder.SeedAsync(cancellationToken);
        logger.LogInformation("Development auth data seed finished");
    }
}
