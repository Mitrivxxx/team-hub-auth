using Microsoft.Extensions.Options;
using team_hub_auth.Configuration;

namespace team_hub_auth.Tests.Configuration;

internal static class TestJwtConfiguration
{
    public const string Key = "super-secret-test-key-that-is-long-enough";
    public const string Issuer = "TeamHubTests";
    public const string Audience = "TeamHubTestsAudience";
    public const int AccessTokenExpireMinutes = 15;

    public static IOptions<JwtOptions> CreateJwtOptions(int? expireMinutesOverride = null)
    {
        return Options.Create(new JwtOptions
        {
            Key = Key,
            Issuer = Issuer,
            Audience = Audience,
            ExpireMinutes = expireMinutesOverride ?? AccessTokenExpireMinutes
        });
    }
}

