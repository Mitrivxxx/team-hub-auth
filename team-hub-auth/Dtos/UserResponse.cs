namespace team_hub_auth.Dtos;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string Name { get; set; } = "";
    public string Surname { get; set; } = "";
    public string Role { get; set; } = "";
}
