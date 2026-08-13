namespace team_hub_auth.Dtos;

public sealed class ChangeMyPasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
