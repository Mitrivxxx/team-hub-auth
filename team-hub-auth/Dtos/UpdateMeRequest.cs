namespace team_hub_auth.Dtos;

public sealed class UpdateMeRequest
{
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Email { get; set; }
}
