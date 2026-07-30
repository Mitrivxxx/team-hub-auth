namespace team_hub_auth.Dtos;

public class RegisterRequest
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Surname { get; set; } = "";
    public string Password { get; set; } = "";
}
