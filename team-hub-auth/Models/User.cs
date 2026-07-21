namespace team_hub_auth.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string Name { get; set; } = "";
    public string Surname { get; set; } = "";
    public string Password { get; set; } = "";
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutUntil { get; set; }
    public string? RefreshTokenHash { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
}
