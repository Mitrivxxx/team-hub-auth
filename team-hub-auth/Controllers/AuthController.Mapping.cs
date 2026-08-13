using team_hub_auth.Dtos;
using team_hub_auth.Models;

namespace team_hub_auth.Controllers;

public partial class AuthController
{
    UserResponse ToResponse(User user) => userResponseMapper.Map(user);

    AuthResponse ToAuthResponse(User user, string accessToken, DateTimeOffset accessTokenExpiresAt) => new()
    {
        AccessToken = accessToken,
        ExpiresInSeconds = (int)Math.Max(0, (accessTokenExpiresAt - DateTimeOffset.UtcNow).TotalSeconds),
        User = ToResponse(user)
    };
}
