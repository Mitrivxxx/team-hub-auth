using team_hub_auth.Dtos;
using team_hub_auth.Validators;

namespace team_hub_auth.Tests.Validators;

public class LoginRequestValidatorTests
{
    readonly LoginRequestValidator validator = new();

    [Fact]
    public void Validate_WhenRequestIsValid_ShouldPass()
    {
        var request = new LoginRequest
        {
            Username = "john_doe",
            Password = "secret123"
        };

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenUsernameIsEmpty_ShouldFail(string? username)
    {
        var request = new LoginRequest
        {
            Username = username ?? string.Empty,
            Password = "secret123"
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginRequest.Username));
    }

    [Fact]
    public void Validate_WhenUsernameIsLongerThan30_ShouldFail()
    {
        var request = new LoginRequest
        {
            Username = new string('a', 31),
            Password = "secret123"
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginRequest.Username));
    }

    [Fact]
    public void Validate_WhenPasswordIsEmpty_ShouldFail()
    {
        var request = new LoginRequest
        {
            Username = "john_doe",
            Password = string.Empty
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginRequest.Password));
    }
}
