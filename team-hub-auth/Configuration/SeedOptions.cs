using System.ComponentModel.DataAnnotations;

namespace team_hub_auth.Configuration;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; init; }

    [Range(0, 1_000_000)]
    public int UserCount { get; init; }

    [Range(1, 5_000)]
    public int BatchSize { get; init; } = 500;

    [Required]
    public string EmailDomain { get; init; } = "@teamhub.local";

    [Required]
    public string ActiveUsername { get; init; } = "JanWilk123";

    [Required]
    public string ActivePassword { get; init; } = "janwilk123";
}
