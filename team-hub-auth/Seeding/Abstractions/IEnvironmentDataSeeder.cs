namespace team_hub_auth.Seeding.Abstractions;

public interface IEnvironmentDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
