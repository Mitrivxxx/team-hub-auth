using team_hub_auth.Seeding.Abstractions;
using team_hub_auth.Seeding.Users;

namespace team_hub_auth.Seeding.Staging;

public sealed class StagingDataSeeder(
    ActiveDemoUserSeeder activeUserSeeder,
    BulkDemoUserSeeder bulkUserSeeder,
    ILogger<StagingDataSeeder> logger) : IEnvironmentDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running Staging auth data seed...");
        await activeUserSeeder.EnsureAsync(cancellationToken);
        await bulkUserSeeder.SeedAsync(cancellationToken);
        logger.LogInformation("Staging auth data seed finished");
    }
}
