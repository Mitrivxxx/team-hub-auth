using team_hub_auth.Models;

namespace team_hub_auth.Services.Tokens;

public interface ITokenService
{
    (string token, DateTimeOffset expiresAt) GenerateAccessToken(User user);

    (string token, string hash, DateTimeOffset expiresAt) GenerateRefreshToken();

    string HashRefreshToken(string token);
}
