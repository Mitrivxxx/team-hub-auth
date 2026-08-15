using team_hub_auth.Seeding.Abstractions;
using team_hub_auth.Seeding.Users;

namespace team_hub_auth.Seeding;

public sealed class DemoDataSeeder(
    ActiveDemoUserSeeder activeUserSeeder,
    BulkDemoUserSeeder bulkUserSeeder,
    ILogger<DemoDataSeeder> logger) : IEnvironmentDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running auth demo data seed...");
        await activeUserSeeder.EnsureAsync(cancellationToken);
        await bulkUserSeeder.SeedAsync(cancellationToken);
        logger.LogInformation("Auth demo data seed finished");
    }
}
