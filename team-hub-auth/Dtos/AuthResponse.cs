namespace team_hub_auth.Dtos;

public class AuthResponse
{
    public string AccessToken { get; set; } = "";
    public int ExpiresInSeconds { get; set; }
    public UserResponse User { get; set; } = null!;
}
