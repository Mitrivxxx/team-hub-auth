namespace team_hub_auth.Models;

public class User
{
    public Guid Id { get; set; }
    public UserIdentity Identity { get; set; } = new();
    public UserProfile Profile { get; set; } = new();
    public UserCredentials Credentials { get; set; } = new();
    public UserSecurity Security { get; set; } = new();
}
