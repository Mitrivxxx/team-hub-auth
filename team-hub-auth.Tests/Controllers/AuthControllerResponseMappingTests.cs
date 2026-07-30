using System.Reflection;
using team_hub_auth.Controllers;
using team_hub_auth.Dtos;
using team_hub_auth.Models;

namespace team_hub_auth.Tests.Controllers;

public class AuthControllerResponseMappingTests
{
    [Fact]
    public void ToAuthResponse_WhenAccessTokenExpired_ShouldNotReturnNegativeExpiresInSeconds()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Identity = new UserIdentity
            {
                Username = "john",
                Email = ""
            },
            Profile = new UserProfile
            {
                Name = "John",
                Surname = "Doe"
            },
            Credentials = new UserCredentials
            {
                PasswordHash = AuthControllerTestHelpers.PasswordHasher.Hash("secret123")
            }
        };

        var toAuthResponseMethod = typeof(AuthController).GetMethod(
            "ToAuthResponse",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(toAuthResponseMethod);

        var response = toAuthResponseMethod!.Invoke(
            null,
            [user, "access-token", DateTimeOffset.UtcNow.AddSeconds(-15)]) as AuthResponse;

        Assert.NotNull(response);
        Assert.Equal(0, response.ExpiresInSeconds);
    }
}
