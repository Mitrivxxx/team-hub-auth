using System.ComponentModel.DataAnnotations;

namespace team_hub_auth.Configuration;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;
}
