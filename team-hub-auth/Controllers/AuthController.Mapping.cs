using team_hub_auth.Dtos;
using team_hub_auth.Models;

namespace team_hub_auth.Controllers;

public partial class AuthController
{
    static UserResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Name = user.Name,
        Surname = user.Surname
    };

    static AuthResponse ToAuthResponse(User user, string accessToken, DateTimeOffset accessTokenExpiresAt) => new()
    {
        AccessToken = accessToken,
        ExpiresInSeconds = (int)Math.Max(0, (accessTokenExpiresAt - DateTimeOffset.UtcNow).TotalSeconds),
        User = ToResponse(user)
    };
}
