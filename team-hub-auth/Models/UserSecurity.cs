namespace team_hub_auth.Models;

public sealed class UserSecurity
{
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutUntil { get; set; }
    public string? RefreshTokenHash { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
}

