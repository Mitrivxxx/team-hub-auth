using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using team_hub_auth.Configuration;
using team_hub_auth.Models;
using team_hub_auth.Services;

namespace team_hub_auth.Tests.Services;

public class TokenServiceTests
{
    readonly TokenService service;

    public TokenServiceTests()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Key = "super-secret-test-key-that-is-long-enough",
            Issuer = "TeamHubTests",
            Audience = "TeamHubTestsAudience",
            ExpireMinutes = 15
        });

        service = new TokenService(jwtOptions);
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainExpectedMetadataAndClaims()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "john_doe"
        };

        var before = DateTimeOffset.UtcNow;
        var (token, expiresAt) = service.GenerateAccessToken(user, "admin");
        var after = DateTimeOffset.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("TeamHubTests", jwt.Issuer);
        Assert.Equal("TeamHubTestsAudience", jwt.Audiences.Single());
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == user.Username);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "admin");
        Assert.InRange(expiresAt, before.AddMinutes(15).AddSeconds(-2), after.AddMinutes(15).AddSeconds(2));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnTokenHashAndExpectedExpiration()
    {
        var before = DateTimeOffset.UtcNow;
        var (token, hash, expiresAt) = service.GenerateRefreshToken();
        var after = DateTimeOffset.UtcNow;

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash, service.HashRefreshToken(token));
        Assert.InRange(
            expiresAt,
            before.AddDays(TokenService.RefreshTokenDays).AddSeconds(-2),
            after.AddDays(TokenService.RefreshTokenDays).AddSeconds(2));
    }

    [Fact]
    public void HashRefreshToken_ShouldBeDeterministic()
    {
        const string token = "refresh-token-value";

        var hash1 = service.HashRefreshToken(token);
        var hash2 = service.HashRefreshToken(token);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length);
    }
}
