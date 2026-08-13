namespace team_hub_auth.Models;

public sealed class UserProfile
{
    public string Name { get; set; } = "";
    public string Surname { get; set; } = "";
    public string? AvatarUrl { get; set; }
}

